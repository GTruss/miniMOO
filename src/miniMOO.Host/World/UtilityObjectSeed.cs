using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUtilityObjects(InMemoryObjectRepository repo) {
        AddGenericUtilitiesPackage(repo);
        AddStringUtils(repo);
        AddBuildingUtils(repo);
        AddObjectUtils(repo);
        AddListUtils(repo);
        AddCodeUtils(repo);
    }

    // ── Generic Utilities Package (#78) ───────────────────────────
    private static void AddGenericUtilitiesPackage(InMemoryObjectRepository repo) {
        var genUtils = Obj(WorldIds.GenericUtilitiesPackage, ObjectId.System, WorldIds.Root, null, "Generic Utilities Package");
        genUtils.Flags = ObjectFlags.Readable;
        genUtils.Verbs.Add(ScriptVerb(["help"], """
            return "This is a collection of utility verbs for use in your MOO. It includes:\n\n" +
                   "- $string_utils: string manipulation verbs\n" +
                   "- $building_utils: object creation and building helper verbs\n" +
                   "- $object_utils: general-purpose object query verbs";
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));
        repo.Add(genUtils);
    }

    // ── $string_utils (#20) ───────────────────────────────────────

    private static void AddStringUtils(InMemoryObjectRepository repo) {
        var su = Obj(WorldIds.StringUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$string_utils");
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
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["regexp_quote"], """
            string = args[1];
            quoted = "";
            while (m = rmatch(string, "[][$^.*+?%].*"))
              quoted = "%" + string[m[1]..m[2]] + quoted;
              string = string[1..m[1] - 1];
            endwhile
            return string + quoted;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["index_delimited", "index_d"], """
            args[2] = "%(%W%|^%)" + $string_utils:regexp_quote(args[2]) + "%(%W%|$%)";
            return (m = match(@args)) ? m[3][1][2] + 1 | 0;       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["from_list"], """
            {thelist, ?separator = ""} = args;
            if (separator == "")
              return tostr(@thelist);
            elseif (thelist)
              result = tostr(thelist[1]);
              for elt in (listdelete(thelist, 1))
                result = tostr(result, separator, elt);
              endfor
              return result;
            else
              return "";
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["from_value"], """
            {value, ?quote_strings = 0, ?list_depth = 1} = args;
            if (typeof(value) == LIST)
              if (value)
                if (list_depth)
                  result = "{" + this:from_value(value[1], quote_strings, list_depth - 1);
                  for v in (listdelete(value, 1))
                    result = tostr(result, ", ", this:from_value(v, quote_strings, list_depth - 1));
                  endfor
                  return result + "}";
                else
                  return "{...}";
                endif
              else
                return "{}";
              endif
            elseif (quote_strings)
              if (typeof(value) == STR)
                result = "\"";
                while (q = index(value, "\"") || index(value, "\\"))
                  if (value[q] == "\"")
                    q = min(q, index(value + "\\", "\\"));
                  endif
                  result = result + value[1..q - 1] + "\\" + value[q];
                  value = value[q + 1..$];
                endwhile
                return result + value + "\"";
              elseif (typeof(value) == ERR)
                return $code_utils:error_name(value);
              else
                return tostr(value);
              endif
            else
              return tostr(value);
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["char_list"], """
            if (30 < (len = length(string = args[1])))
              return {@this:char_list(string[1..$ / 2]), @this:char_list(string[$ / 2 + 1..$])};
            else
              l = {};
              for c in [1..len]
                l = {@l, string[c]};
              endfor
              return l;
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        repo.Add(su);
    } 

    // ── $building_utils (#21) ─────────────────────────────────────

    private static void AddBuildingUtils(InMemoryObjectRepository repo) {
        var bu = Obj(WorldIds.BuildingUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$building_utils");
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
            {spec, source, dest, ?use_recycler, ?exit_kind = $exit} = args;
            exit = player:_create(exit_kind);
            if (typeof(exit) == ERR)
              player:notify(tostr("Cannot create new exit as a child of ", $string_utils:nn(exit_kind), ": ", exit, ".  See `help @build-options' for information on how to specify the kind of exit this command tries to create."));
              return;
            endif
            for f in ($string_utils:char_list(player:build_option("create_flags") || ""))
              exit.(f) = 1;
            endfor
            $building_utils:set_names(exit, spec);
            exit.source = source;
            exit.dest = dest;
            source_ok = source:add_exit(exit);
            dest_ok = dest:add_entrance(exit);
            move(exit, $nothing);
            via = $string_utils:from_value(setadd(exit.aliases, exit.name), 1);
            if (source_ok)
              player:tell("Exit from ", source.name, " (", source, ") to ", dest.name, " (", dest, ") via ", via, " created with id ", exit, ".");
              if (!dest_ok)
                player:tell("However, I couldn't add ", exit, " as a legal entrance to ", dest.name, ".  You may have to get its owner, ", dest.owner.name, " to add it for you.");
              endif
              return {exit};
            elseif (dest_ok)
              player:tell("Exit to ", dest.name, " (", dest, ") via ", via, " created with id ", exit, ".  However, I couldn't add ", exit, " as a legal exit from ", source.name, ".  Get its owner, ", source.owner.name, " to add it for you.");
              return {exit};
            else
              "player:_recycle(exit);";
              player:tell("I couldn't add a new exit as EITHER a legal exit from ", source.name, " OR as a legal entrance to ", dest.name, ".  Get their owners, ", source.owner.name, " and ", dest.owner.name, ", respectively, to add it for you.");
              return 0;
            endif
        """));

        repo.Add(bu);
    }

    // ── $object_utils (#52) ───────────────────────────────────────

    private static void AddObjectUtils(InMemoryObjectRepository repo) {
        var genObjectUtils = Obj(WorldIds.ObjectUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$object_utils");
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

    // ── $list_utils (#55) ───────────────────────────────────────

    private static void AddListUtils(InMemoryObjectRepository repo) {
        var lu = Obj(WorldIds.ListUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$list_utils");
        lu.Flags = ObjectFlags.Readable;


        lu.Verbs.Add(ScriptVerb(["assoc"], """
            {target, thelist, ?indx = 1} = args;
            for t in (thelist)
              if (typeof(t) == LIST && `t[indx] == target ! E_RANGE => 0')
                return t;
              endif
            endfor
            return {};
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));


        repo.Add(lu);

    }

    // ── $code_utils (#59) ───────────────────────────────────────

    private static void AddCodeUtils(InMemoryObjectRepository repo) {
        var cu = Obj(WorldIds.CodeUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$code_utils");
        cu.Flags = ObjectFlags.Readable;


        cu.Verbs.Add(ScriptVerb(["error_name"], """
            return toliteral(@args);
            return this.error_names[toint(args[1]) + 1];
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));


        repo.Add(cu);

    }

}
