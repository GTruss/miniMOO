using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUtilityObjects(InMemoryObjectRepository repo) {
        AddGenericUtilitiesPackage(repo);
        AddStringUtils(repo);
        AddBuildingUtils(repo);
        AddGenderUtils(repo);
        AddObjectUtils(repo);
        AddListUtils(repo);
        AddCodeUtils(repo);
        AddCommandUtils(repo);
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

        su.Verbs.Add(ScriptVerb(["capitalize", "capitalise"], """
            string = args[1];
            if (string && (i = index("abcdefghijklmnopqrstuvwxyz", string[1], 1)))
              string[1] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i];
            endif
            return string;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["name_and_number", "nn", "name_and_number_list", "nn_list"], """
            {objs, ?sepr = " ", @eng_args} = args;
            if (typeof(objs) != LIST)
              objs = {objs};
            endif
            name_list = {};
            for what in (objs)
              name = valid(what) ? what.name | {"<invalid>", "$nothing", "$ambiguous_match", "$failed_match"}[1 + (what in {#-1, #-2, #-3})];
              name = tostr(name, sepr, "(", what, ")");
              name_list = {@name_list, name};
            endfor
            return this:english_list(name_list, @eng_args);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["match_player"], """
            retstr = 0;
            me = player;
            if (length(args) < 2 || typeof(me = args[2]) == OBJ)
              me = valid(me) && is_player(me) ? me | $failed_match;
              if (typeof(args[1]) == STR)
                strings = {args[1]};
                retstr = 1;
                "return a string, not a list";
              else
                strings = args[1];
              endif
            else
              strings = args;
              me = player;
            endif
            found = {};
            for astr in (strings)
              if (!astr)
                aobj = $nothing;
              elseif (astr == "me")
                aobj = me;
              elseif (valid(aobj = $string_utils:literal_object(astr)) && is_player(aobj))
                "astr is a valid literal object number of some player, so we are done.";
              else
                aobj = $player_db:find(astr);
              endif
              found = {@found, aobj};
            endfor
            return retstr ? found[1] | found;      
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["match"], """
            subject = args[1];
            if (subject == "")
              return $nothing;
            endif
            no_exact_match = no_partial_match = 1;
            for i in [1..length(args) / 2]
              prop_name = args[2 * i + 1];
              for object in (typeof(olist = args[2 * i]) == LIST ? olist | {olist})
                if (valid(object))
                  if (typeof(str_list = `object.(prop_name) ! E_PERM, E_PROPNF => {}') != LIST)
                    str_list = {str_list};
                  endif
                  if (subject in str_list)
                    if (no_exact_match)
                      no_exact_match = object;
                    elseif (no_exact_match != object)
                      return $ambiguous_match;
                    endif
                  else
                    for string in (str_list)
                      if (index(string, subject) != 1)
                      elseif (no_partial_match)
                        no_partial_match = object;
                      elseif (no_partial_match != object)
                        no_partial_match = $ambiguous_match;
                      endif
                    endfor
                  endif
                endif
              endfor
            endfor
            return no_exact_match && (no_partial_match && $failed_match);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["match_object"], """
            {string, here, ?who = player} = args;
            if ($failed_match != (object = this:literal_object(string)))
              return object;
            elseif (string == "me")
              return who;
            elseif (string == "here")
              return here;
            elseif (valid(pobject = who:match(string)) && string in {@pobject.aliases, pobject.name} || !valid(here))
              "...exact match in player or room is bogus...";
              return pobject;
            elseif (valid(hobject = here:match(string)) && string in {@hobject.aliases, hobject.name} || pobject == $failed_match)
              "...exact match in room or match in player failed completely...";
              return hobject;
            else
              return pobject;
            endif       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        su.Verbs.Add(ScriptVerb(["literal_object"], """
            string = args[1];
            if (!string)
              return $nothing;
            elseif (string[1] == "#" && E_TYPE != (object = $code_utils:toobj(string)))
              return object;
            elseif (string[1] == "~")
              return this:match_player(string[2..$], #0);
            elseif (string[1] == "$")
              string[1..1] = "";
              object = #0;
              while (pn = string[1..(dot = index(string, ".")) ? dot - 1 | $])
                if (!$object_utils:has_property(object, pn) || typeof(object = object.(pn)) != OBJ)
                  return $failed_match;
                endif
                string = string[length(pn) + 2..$];
              endwhile
              if (object == #0 || typeof(object) == ERR)
                return $failed_match;
              else
                return object;
              endif
            else
              return $failed_match;
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

    private static void AddGenderUtils(InMemoryObjectRepository repo) {
        var gu = Obj(WorldIds.GenderUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$gender_utils");
        gu.Flags = ObjectFlags.Readable;

        Prop(gu, "genders", StrList(
            "neuter", "male", "female", "either", "spivak",
            "splat", "plural", "egotistical", "royal", "2nd"));
        Prop(gu, "is_plural", IntList(0, 0, 0, 0, 0, 0, 1, 0, 1, 0));
        Prop(gu, "have", StrList(
            "has", "has", "has", "has", "has",
            "has", "have", "have", "have", "have"));
        Prop(gu, "be", StrList(
            "is", "is", "is", "is", "is",
            "is", "are", "are", "are", "are"));

        gu.Verbs.Add(ScriptVerb(["get_conj", "get_conjugation"], """
            {spec, ?object = player} = args;
            i = index(spec + "/", "/");
            sing = spec[1..i - 1];
            if (i < length(spec))
              plur = spec[i + 1..$];
            else
              plur = "";
            endif
            cap = "a" > ((i == 1) ? spec[2] | spec);
            if (((valid(object) && (STR == typeof(g = `object.gender ! ANY => ""'))) && (i = g in this.genders)) && this.is_plural[i])
              vb = plur || this:_verb_plural(sing, i);
            else
              vb = sing || this:_verb_singular(plur, i);
            endif
            if (cap)
              return $string_utils:capitalize(vb);
            else
              return vb;
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        gu.Verbs.Add(ScriptVerb(["_verb_plural"], """
            {st, idx} = args;
            if (typeof(st) != STR)
              return E_INVARG;
            endif
            len = length(st);
            if ((len >= 3) && (st[len - 2..$] == "n't"))
              return this:_verb_plural(st[1..len - 3], idx) + "n't";
            elseif (i = st in {"has", "is"})
              return this.({"have", "be"}[i])[idx];
            elseif (st == "was")
              return (idx > 6) ? "were" | st;
            elseif ((len <= 3) || (st[len] != "s"))
              return st;
            elseif (st[len - 1] != "e")
              return st[1..len - 1];
            elseif ((len >= 4) && (st[len - 3..$] == "zzes"))
              return st[1..len - 3];
            elseif ((len >= 4) && ((((st[len - 2] == "h") && index("cs", st[len - 3])) || index("ox", st[len - 2])) || (st[len - 3..len - 2] == "ss")))
              return st[1..len - 2];
            elseif (st[len - 2] == "i")
              return st[1..len - 3] + "y";
            else
              return st[1..len - 1];
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        gu.Verbs.Add(ScriptVerb(["_verb_singular"], """
            {st, ?idx = 1} = args;
            if (typeof(st) != STR)
              return E_INVARG;
            endif
            len = length(st);
            if (!len)
              return "";
            elseif ((len >= 3) && (st[len - 2..$] == "n't"))
              return this:_verb_singular(st[1..len - 3], idx) + "n't";
            elseif (i = st in {"have", "are"})
              return this.({"have", "be"}[i])[idx];
            elseif ((len > 1) && (st[len] == "y") && (!index("aeiou", st[len - 1])))
              return st[1..len - 1] + "ies";
            elseif ((len > 1) && index("sz", st[len]) && index("aeiou", st[len - 1]))
              return (st + st[len]) + "es";
            elseif (index("osx", st[len]) || ((len > 1) && (index("chsh", st[len - 1..len]) % 2)))
              return st + "es";
            else
              return st + "s";
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        repo.Add(gu);
    }

    private static void AddObjectUtils(InMemoryObjectRepository repo) {
        var genObjectUtils = Obj(WorldIds.ObjectUtils, ObjectId.System, WorldIds.GenericUtilitiesPackage, null, "$object_utils");
        genObjectUtils.Flags = ObjectFlags.Readable;

        genObjectUtils.Verbs.Add(ScriptVerb(["has_callable_verb"], """
            {object, verbname} = args;
            while (valid(object))
              if (`index(verb_info(object, verbname)[2], "x") ! E_VERBNF => 0' && verb_code(object, verbname))
                return {object};
              endif
              object = parent(object);
            endwhile
            return 0;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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

        genObjectUtils.Verbs.Add(ScriptVerb(["has_property"], """
            {object, prop} = args;
            try
              object.(prop);
              return 1;
            except (E_PROPNF, E_INVIND)
              return 0;
            endtry       
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

        cu.Verbs.Add(ScriptVerb(["toobj"], """
            return match(s = args[1], "^ *#[-+]?[0-9]+ *$") ? toobj(s) | E_TYPE;;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));


        repo.Add(cu);

    }

    // ── $command_utils (#56) ───────────────────────────────────────

    private static void AddCommandUtils(InMemoryObjectRepository repo) {
        var cu = Obj(WorldIds.CommandUtils, WorldIds.Wizard, WorldIds.GenericUtilitiesPackage, null, "$command_utils");
        cu.Flags = ObjectFlags.Readable;

        cu.Verbs.Add(ScriptVerb(["object_match_failed"], """
            {match_result, string} = args;
            
            if (index(string, "#") == 1 && $code_utils:toobj(string) != E_TYPE)
              "...avoid the `I don't know which `#-2' you mean' message...";
              if (!valid(match_result))
                player:tell(tostr(string, " does not exist."));
              endif
              return !valid(match_result);
            elseif (match_result == $nothing)
              player:tell("You must give the name of some object.");
            elseif (match_result == $failed_match)
              player:tell(tostr("I see no \"", string, "\" here."));
            elseif (match_result == $ambiguous_match)
              player:tell(tostr("I don't know which \"", string, "\" you mean."));
            elseif (!valid(match_result))
              player:tell(tostr(match_result, " does not exist."));
            else
              return 0;
            endif
            return 1;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));


        repo.Add(cu);

    }

}
