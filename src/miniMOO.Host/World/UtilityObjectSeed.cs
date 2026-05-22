using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUtilityObjects(InMemoryObjectRepository repo) {
        AddStringUtils(repo);
        AddBuildingUtils(repo);
        AddObjectUtils(repo);
    }

    // ── $string_utils (#20) ───────────────────────────────────────

    private static void AddStringUtils(InMemoryObjectRepository repo) {
        var su = Obj(WorldIds.StringUtils, ObjectId.System, null, null, "$string_utils");
        su.Flags = ObjectFlags.Readable;

        // explode(str, sep) => list of substrings separated by sep
        su.Verbs.Add(ScriptVerb(["explode"], """
            str = args[1];
            sep = args[2];
            result = {};
            done = 0;
            while (!done)
              i = index(str, sep);
              if (i == 0)
                if (length(str) > 0)
                  result = listappend(result, str);
                endif
                done = 1;
              else
                if (i > 1)
                  result = listappend(result, substr(str, 1, i - 1));
                endif
                str = substr(str, i + length(sep));
              endif
            endwhile
            return result;
        """));


        su.Verbs.Add(ScriptVerb(["english_list"], """
            {things, ?nothingstr = "nothing", ?andstr = " and ", ?commastr = ", ", ?finalcommastr = ","} = args;
            nthings = length(things);
            if (nthings == 0)
              return nothingstr;
            elseif (nthings == 1)
              return tostr(things[1]);
            elseif (nthings == 2)
              return tostr(things[1], andstr, things[2]);
            else
              ret = "";
              for k in [1..nthings - 1]
                if (k == nthings - 1)
                  commastr = finalcommastr;
                endif
                ret = tostr(ret, things[k], commastr);
              endfor
              return tostr(ret, andstr, things[nthings]);
            endif
        """));

        repo.Add(su);
    }

    // ── $building_utils (#21) ─────────────────────────────────────

    private static void AddBuildingUtils(InMemoryObjectRepository repo) {
        var bu = Obj(WorldIds.BuildingUtils, ObjectId.System, null, null, "$building_utils");
        bu.Flags = ObjectFlags.Readable;

        // parse_names(spec) => {name, {alias, alias, ...}}
        // spec can be "name,alias,..." or "name:alias,alias,..."
        bu.Verbs.Add(ScriptVerb(["parse_names"], """
            spec = args[1];
            colon = index(spec, ":");
            if (colon)
              name = substr(spec, 1, colon - 1);
              alias_str = substr(spec, colon + 1);
            else
              name = "";
              alias_str = spec;
            endif
            aliases = $string_utils:explode(alias_str, ",");
            if (name == "")
              name = aliases[1];
            endif
            return {name, aliases};
        """));

        // set_names(obj, spec) — sets name and all aliases on obj
        bu.Verbs.Add(ScriptVerb(["set_names"], """
            obj = args[1];
            names = this:parse_names(args[2]);
            name = names[1];
            aliases = names[2];
            set_name(obj, name);
            i = 1;
            while (i <= length(aliases))
              add_alias(obj, aliases[i]);
              i = i + 1;
            endwhile
        """));

        // make_exit(spec, source, dest)
        // Creates a child of $exit with direction verb(s), placed in source, pointing to dest.
        bu.Verbs.Add(ScriptVerb(["make_exit"], """
            spec   = args[1];
            source = args[2];
            dest   = args[3];

            exit_obj = create($exit);
            this:set_names(exit_obj, spec);
            exit_obj.source = source;
            exit_obj.destination = dest;

            move(exit_obj, source);

            source.exits = setadd(source.exits, exit_obj);
            dest.entrances = setadd(dest.entrances, exit_obj);
            verb_names = spec;
            colon = index(verb_names, ":");

            if (colon)
              verb_names = substr(verb_names, 1, colon - 1) + "," + substr(verb_names, colon + 1);
            endif
            add_verb(exit_obj, verb_names, "move(player, this.destination); player.location:look_self();");
            player:tell("Exit ", exit_obj.name, " (", exit_obj, ") to ", dest.name, " (", dest, ") created.");
            return exit_obj;
        """));

        repo.Add(bu);
    }

    // ── $object_utils (#52) ───────────────────────────────────────

    private static void AddObjectUtils(InMemoryObjectRepository repo) {
        var genObjectUtils = Obj(WorldIds.ObjectUtils, ObjectId.System, null, null, "$object_utils");
        genObjectUtils.Flags = ObjectFlags.Readable;

        genObjectUtils.Verbs.Add(ScriptVerb(["ancestors"], """
            ret = {};
            for o in (args)
              what = o;
              while (valid(what = parent(what)))
                ret = setadd(ret, what);
              endwhile
            endfor
            return ret;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["isa"], """
            what = args[1];
            targ = args[2];
            while (valid(what))
              if (what == targ)
                return 1;
              endif
              what = parent(what);
            endwhile
            return 0;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["contains"], """
            loc = args[1];
            what = args[2];
            while (valid(what))
              what = what.location;
              if (what == loc)
                return 1;
              endif
            endwhile
            return 0;
        """));

        genObjectUtils.Verbs.Add(ScriptVerb(["locations"], """
            ret = {};
            what = args[1];
            while (valid(what = what.location))
              ret = {@ret, what};
            endwhile
            return ret;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["descendants", "descendents"], """
            r = children(args[1]);
            i = 1;
            while (i <= length(r))
              kids = children(r[i]);
              if (kids)
                r = {@r, @kids};
              endif
              i = i + 1;
            endwhile
            return r;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["isoneof"], """
            what = args[1];
            targ = args[2];
            while (valid(what))
              i = 1;
              while (i <= length(targ))
                if (what == targ[i])
                  return 1;
                endif
                i = i + 1;
              endwhile
              what = parent(what);
            endwhile
            return 0;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["isoneof"], """
            what = args[1];
            targ = args[2];
            while (valid(what))
              i = 1;
              while (i <= length(targ))
                if (what == targ[i])
                  return 1;
                endif
                i = i + 1;
              endwhile
              what = parent(what);
            endwhile
            return 0;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genObjectUtils.Verbs.Add(ScriptVerb(["has_verb"], """
            ":has_verb(OBJ object, STR verbname)";
            "Find out if an object has a verb matching the given verbname.";
            "Returns {location} if so, 0 if not, where location is the object or the ancestor on which the verb is actually defined.";
            {object, verbname} = args;
            while (valid(object))
              try
                if (verb_info(object, verbname))
                  return {object};
                endif
              except (E_VERBNF)
                object = parent(object);
              endtry
            endwhile
            return 0;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        repo.Add(genObjectUtils);
    }
}
