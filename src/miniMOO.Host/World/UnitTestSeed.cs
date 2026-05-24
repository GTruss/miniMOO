using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUnitTests(InMemoryObjectRepository repo) {
        var wiz = repo.Get(WorldIds.Wiz);
        if (wiz is null)
            return;

        wiz.Verbs.Add(ScriptVerb(["@parsefail1"], """
            player:tell("before parse failure");
            value = "unterminated;
            player:tell("after parse failure");
        """));

        wiz.Verbs.Add(ScriptVerb(["@parsefail2"], """
            player:tell("before parse failure");
            value = 1 + ;
            player:tell("after parse failure");
        """));

        wiz.Verbs.Add(ScriptVerb(["_test_destructure"], """
            {a, b} = args;
            return (a == "left" && b == "right");
        """));

        wiz.Verbs.Add(ScriptVerb(["@test"], """
            player:tell("Running miniMOO tests...");

            passed = 0;
            failed = 0;

            test_obj = create($thing);
            prop = "dynamic_test";
            test_obj.(prop) = 42;

            if (test_obj.(prop) == 42)
              player:tell("PASS: dynamic property access");
              passed = passed + 1;
            else
              player:tell("FAIL: dynamic property access");
              failed = failed + 1;
            endif

            result = index("hello", "ll");
            if (result == 3)
              player:tell("PASS: index()");
              passed = passed + 1;
            else
              player:tell("FAIL: index() -- expected 3 got ", result);
              failed = failed + 1;
            endif

            result = substr("hello", 2, 3);
            if (result == "ell")
              player:tell("PASS: substr()");
              passed = passed + 1;
            else
              player:tell("FAIL: substr() -- expected ell got ", result);
              failed = failed + 1;
            endif

            if ("abcdef"[2..4] == "bcd")
              player:tell("PASS: string slicing");
              passed = passed + 1;
            else
              player:tell("FAIL: string slicing");
              failed = failed + 1;
            endif

            slice = {10, 20, 30, 40}[2..3];
            if (length(slice) == 2 && slice[1] == 20 && slice[2] == 30)
              player:tell("PASS: list slicing");
              passed = passed + 1;
            else
              player:tell("FAIL: list slicing");
              failed = failed + 1;
            endif

            if ("abc"[1..0] == "")
              player:tell("PASS: empty string slice");
              passed = passed + 1;
            else
              player:tell("FAIL: empty string slice");
              failed = failed + 1;
            endif

            if ({"a", "b", "c"}[$] == "c")
              player:tell("PASS: list $ index");
              passed = passed + 1;
            else
              player:tell("FAIL: list $ index");
              failed = failed + 1;
            endif

            slice = {"a", "b", "c", "d"}[2..$];
            if (length(slice) == 3 && slice[1] == "b" && slice[3] == "d")
              player:tell("PASS: list $ slice");
              passed = passed + 1;
            else
              player:tell("FAIL: list $ slice");
              failed = failed + 1;
            endif

            if ("hello"[$] == "o" && "hello"[2..$ - 1] == "ell")
              player:tell("PASS: string $ index/slice");
              passed = passed + 1;
            else
              player:tell("FAIL: string $ index/slice");
              failed = failed + 1;
            endif

            word = "staff";
            word[1..1] = "";
            if (word == "taff")
              player:tell("PASS: string slice assignment delete");
              passed = passed + 1;
            else
              player:tell("FAIL: string slice assignment delete");
              failed = failed + 1;
            endif

            word = "stff";
            word[3..2] = "a";
            if (word == "staff")
              player:tell("PASS: string slice assignment insert");
              passed = passed + 1;
            else
              player:tell("FAIL: string slice assignment insert");
              failed = failed + 1;
            endif

            items = {"a", "x", "d"};
            items[2..2] = {"b", "c"};
            if (length(items) == 4 && items[2] == "b" && items[3] == "c")
              player:tell("PASS: list slice assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: list slice assignment");
              failed = failed + 1;
            endif

            items = {"a", "b", "c"};
            items[2] = "B";
            if (items[1] == "a" && items[2] == "B" && items[3] == "c")
              player:tell("PASS: list indexed assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: list indexed assignment");
              failed = failed + 1;
            endif

            word = "cat";
            word[2] = "u";
            if (word == "cut")
              player:tell("PASS: string indexed assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: string indexed assignment");
              failed = failed + 1;
            endif

            test_obj = create($thing);
            test_obj.description = {"old", "value"};
            test_obj.description[2] = "new";
            if (test_obj.description[1] == "old" && test_obj.description[2] == "new")
              player:tell("PASS: property indexed assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: property indexed assignment");
              failed = failed + 1;
            endif

            result = "b" in {"a", "b", "c"};
            if (result == 2)
              player:tell("PASS: in operator found");
              passed = passed + 1;
            else
              player:tell("FAIL: in operator found -- expected 2 got ", result);
              failed = failed + 1;
            endif

            result = "z" in {"a", "b", "c"};
            if (result == 0)
              player:tell("PASS: in operator not found");
              passed = passed + 1;
            else
              player:tell("FAIL: in operator not found -- expected 0 got ", result);
              failed = failed + 1;
            endif

            caught = 0;
            try
              result = "z" in "not a list";
            except e (E_TYPE)
              caught = e;
            endtry
            if (caught == E_TYPE)
              player:tell("PASS: in operator E_TYPE");
              passed = passed + 1;
            else
              player:tell("FAIL: in operator E_TYPE");
              failed = failed + 1;
            endif

            caught = 0;
            try
              result = {"a"}[2];
            except e (E_RANGE)
              caught = e;
            endtry
            if (caught == E_RANGE)
              player:tell("PASS: list index E_RANGE");
              passed = passed + 1;
            else
              player:tell("FAIL: list index E_RANGE");
              failed = failed + 1;
            endif

            caught = 0;
            try
              result = length(1);
            except e (E_TYPE)
              caught = e;
            endtry
            if (caught == E_TYPE)
              player:tell("PASS: length() E_TYPE");
              passed = passed + 1;
            else
              player:tell("FAIL: length() E_TYPE");
              failed = failed + 1;
            endif

            caught = 0;
            try
              result = 1 / 0;
            except e (E_DIV)
              caught = e;
            endtry
            if (caught == E_DIV)
              player:tell("PASS: division E_DIV");
              passed = passed + 1;
            else
              player:tell("FAIL: division E_DIV");
              failed = failed + 1;
            endif

            result = setadd({"a"}, "b");
            if (length(result) == 2 && result[2] == "b")
              player:tell("PASS: setadd()");
              passed = passed + 1;
            else
              player:tell("FAIL: setadd()");
              failed = failed + 1;
            endif

            result = setadd({"a"}, "a");
            if (length(result) == 1)
              player:tell("PASS: setadd() duplicate");
              passed = passed + 1;
            else
              player:tell("FAIL: setadd() duplicate");
              failed = failed + 1;
            endif

            removed = setremove({1, 2, 3}, 2);
            if (length(removed) == 2 && removed[1] == 1 && removed[2] == 3)
              player:tell("PASS: setremove()");
              passed = passed + 1;
            else
              player:tell("FAIL: setremove()");
              failed = failed + 1;
            endif

            removed = setremove({1, 2, 3}, 4);
            if (length(removed) == 3 && removed[1] == 1 && removed[2] == 2 && removed[3] == 3)
              player:tell("PASS: setremove() missing value");
              passed = passed + 1;
            else
              player:tell("FAIL: setremove() missing value");
              failed = failed + 1;
            endif

            removed = setremove({1, 2, 3, 2}, 2);
            if (length(removed) == 3 && removed[1] == 1 && removed[2] == 3 && removed[3] == 2)
              player:tell("PASS: setremove() removes first match");
              passed = passed + 1;
            else
              player:tell("FAIL: setremove() removes first match");
              failed = failed + 1;
            endif

            m = rmatch("foobar", "o*b");
            if (m[1] == 4 && m[2] == 4)
              player:tell("PASS: rmatch()");
              passed = passed + 1;
            else
              player:tell("FAIL: rmatch()");
              failed = failed + 1;
            endif

            m = match("foobar", "o+b");
            if (m[1] == 2 && m[2] == 4)
              player:tell("PASS: match()");
              passed = passed + 1;
            else
              player:tell("FAIL: match()");
              failed = failed + 1;
            endif

            m = match("foobar", "z+");
            if (length(m) == 0)
              player:tell("PASS: match() not found");
              passed = passed + 1;
            else
              player:tell("FAIL: match() not found");
              failed = failed + 1;
            endif

            result = listappend({"a"}, "a");
            if (length(result) == 2 && result[2] == "a")
              player:tell("PASS: listappend()");
              passed = passed + 1;
            else
              player:tell("FAIL: listappend()");
              failed = failed + 1;
            endif

            deleted = listdelete({"north", "east", "south"}, 2);
            if (length(deleted) == 2 && deleted[1] == "north" && deleted[2] == "south")
              player:tell("PASS: listdelete()");
              passed = passed + 1;
            else
              player:tell("FAIL: listdelete()");
              failed = failed + 1;
            endif

            caught = 0;
            try
              listdelete({"north"}, 2);
            except e (E_RANGE)
              caught = e;
            endtry
            if (caught == E_RANGE)
              player:tell("PASS: listdelete() range error");
              passed = passed + 1;
            else
              player:tell("FAIL: listdelete() range error");
              failed = failed + 1;
            endif

            result = "first" || "second";
            if (result == "first")
              player:tell("PASS: || preserves truthy value");
              passed = passed + 1;
            else
              player:tell("FAIL: || preserves truthy value -- got ", result);
              failed = failed + 1;
            endif

            result = "" || "fallback";
            if (result == "fallback")
              player:tell("PASS: || returns fallback value");
              passed = passed + 1;
            else
              player:tell("FAIL: || returns fallback value -- got ", result);
              failed = failed + 1;
            endif

            result = 0 && "right";
            if (result == 0)
              player:tell("PASS: && preserves falsey value");
              passed = passed + 1;
            else
              player:tell("FAIL: && preserves falsey value -- got ", result);
              failed = failed + 1;
            endif

            result = 1 && "right";
            if (result == "right")
              player:tell("PASS: && returns right value");
              passed = passed + 1;
            else
              player:tell("FAIL: && returns right value -- got ", result);
              failed = failed + 1;
            endif

            if (length("hello") == 5)
              player:tell("PASS: length() string");
              passed = passed + 1;
            else
              player:tell("FAIL: length() string");
              failed = failed + 1;
            endif

            if (length({1, 2, 3}) == 3)
              player:tell("PASS: length() list");
              passed = passed + 1;
            else
              player:tell("FAIL: length() list");
              failed = failed + 1;
            endif

            if ("abc"[2] == "b")
              player:tell("PASS: string indexing");
              passed = passed + 1;
            else
              player:tell("FAIL: string indexing");
              failed = failed + 1;
            endif

            if ({10, 20, 30}[3] == 30)
              player:tell("PASS: list indexing");
              passed = passed + 1;
            else
              player:tell("FAIL: list indexing");
              failed = failed + 1;
            endif

            sum = 0;
            for i in [1..4]
              sum = sum + i;
            endfor
            if (sum == 10)
              player:tell("PASS: numeric range for");
              passed = passed + 1;
            else
              player:tell("FAIL: numeric range for -- expected 10 got ", sum);
              failed = failed + 1;
            endif

            sum = 0;
            for x in ({2, 4, 6})
              sum = sum + x;
            endfor
            if (sum == 12)
              player:tell("PASS: list for");
              passed = passed + 1;
            else
              player:tell("FAIL: list for -- expected 12 got ", sum);
              failed = failed + 1;
            endif

            i = 0;
            while (i < 3)
              i = i + 1;
            endwhile
            if (i == 3)
              player:tell("PASS: while loop");
              passed = passed + 1;
            else
              player:tell("FAIL: while loop -- expected 3 got ", i);
              failed = failed + 1;
            endif

            result = this:_test_destructure("left", "right");
            if (result)
              player:tell("PASS: destructuring assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: destructuring assignment");
              failed = failed + 1;
            endif

            {required, ?optional = "fallback"} = {"actual"};
            if (required == "actual" && optional == "fallback")
              player:tell("PASS: optional destructuring default");
              passed = passed + 1;
            else
              player:tell("FAIL: optional destructuring default");
              failed = failed + 1;
            endif

            {first, @rest} = {"a", "b", "c"};
            if (first == "a" && length(rest) == 2 && rest[1] == "b" && rest[2] == "c")
              player:tell("PASS: rest destructuring assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: rest destructuring assignment");
              failed = failed + 1;
            endif

            {only, @empty_rest} = {"a"};
            if (only == "a" && length(empty_rest) == 0)
              player:tell("PASS: empty rest destructuring assignment");
              passed = passed + 1;
            else
              player:tell("FAIL: empty rest destructuring assignment");
              failed = failed + 1;
            endif

            if ("staff" in player.contents[1].aliases)
              player:tell("PASS: aliases builtin property");
              passed = passed + 1;
            else
              player:tell("FAIL: aliases builtin property");
              failed = failed + 1;
            endif

            if (!valid($nothing) && !valid($failed_match) && !valid($ambiguous_match))
              player:tell("PASS: valid() special match objects");
              passed = passed + 1;
            else
              player:tell("FAIL: valid() special match objects");
              failed = failed + 1;
            endif

            if (valid(player))
              player:tell("PASS: valid() player");
              passed = passed + 1;
            else
              player:tell("FAIL: valid() player");
              failed = failed + 1;
            endif

            info = verb_info($root, "tell");
            if (info[1] == #0)
              player:tell("PASS: verb_info() found");
              passed = passed + 1;
            else
              player:tell("FAIL: verb_info() found");
              failed = failed + 1;
            endif

            caught = 0;
            try
              verb_info($root, "definitely_not_a_verb");
            except e (E_VERBNF)
              caught = e;
            endtry
            if (caught == E_VERBNF)
              player:tell("PASS: try/except specific code");
              passed = passed + 1;
            else
              player:tell("FAIL: try/except specific code");
              failed = failed + 1;
            endif

            caught = 0;
            try
              verb_info($root, "definitely_not_a_verb");
            except e (ANY)
              caught = e;
            endtry
            if (caught == E_VERBNF)
              player:tell("PASS: try/except ANY");
              passed = passed + 1;
            else
              player:tell("FAIL: try/except ANY");
              failed = failed + 1;
            endif

            result = `verb_info($root, "definitely_not_a_verb") ! E_VERBNF => "caught"';
            if (result == "caught")
              player:tell("PASS: backtick catch");
              passed = passed + 1;
            else
              player:tell("FAIL: backtick catch");
              failed = failed + 1;
            endif

            code = verb_code($wiz, "_test_destructure");
            found = 0;

            if (typeof(code) == LIST)
              for line in (code)
                if (index(line, "{a, b} = args"))
                  found = 1;
                endif
              endfor
            endif

            if (found)
              player:tell("PASS: verb_code() found");
              passed = passed + 1;
            else
              player:tell("FAIL: verb_code() found");
              failed = failed + 1;
            endif

            caught = 0;
            try
              verb_code(this, "definitely_not_a_verb");
            except (E_VERBNF)
              caught = 1;
            endtry
            if (caught)
              player:tell("PASS: verb_code() not found");
              passed = passed + 1;
            else
              player:tell("FAIL: verb_code() not found");
              failed = failed + 1;
            endif

            if (parent(player) == $wiz)
              player:tell("PASS: parent()");
              passed = passed + 1;
            else
              player:tell("FAIL: parent()");
              failed = failed + 1;
            endif

            kids = children($root);
            if ($room in kids)
              player:tell("PASS: children()");
              passed = passed + 1;
            else
              player:tell("FAIL: children()");
              failed = failed + 1;
            endif

            if (typeof(1) == 0 && typeof(player) == 1 && typeof("x") == 2 && typeof({1}) == 4)
              player:tell("PASS: typeof()");
              passed = passed + 1;
            else
              player:tell("FAIL: typeof()");
              failed = failed + 1;
            endif

            if (typeof(1) == INT && typeof(player) == OBJ && typeof("x") == STR && typeof({1}) == LIST)
              player:tell("PASS: type constants");
              passed = passed + 1;
            else
              player:tell("FAIL: type constants");
              failed = failed + 1;
            endif

            if (tostr("obj=", player) == "obj=#2")
              player:tell("PASS: tostr()");
              passed = passed + 1;
            else
              player:tell("FAIL: tostr()");
              failed = failed + 1;
            endif

            if (toint(34) == 34 && toint(-34) == -34 && toint(player) == 2)
              player:tell("PASS: toint() scalar conversions");
              passed = passed + 1;
            else
              player:tell("FAIL: toint() scalar conversions");
              failed = failed + 1;
            endif

            if (toint("34") == 34 && toint("34.7") == 34 && toint(" - 34  ") == -34 && toint("wat") == 0)
              player:tell("PASS: toint() string conversions");
              passed = passed + 1;
            else
              player:tell("FAIL: toint() string conversions");
              failed = failed + 1;
            endif

            if (toint(E_TYPE) == 1 && tonum(E_PERM) == 3)
              player:tell("PASS: toint() error conversion");
              passed = passed + 1;
            else
              player:tell("FAIL: toint() error conversion");
              failed = failed + 1;
            endif

            if ($string_utils:capitalize("hello") == "Hello" && $string_utils:capitalize("Hello") == "Hello")
              player:tell("PASS: $string_utils:capitalize()");
              passed = passed + 1;
            else
              player:tell("FAIL: $string_utils:capitalize()");
              failed = failed + 1;
            endif

            if ($gender_utils:get_conj("is/are", player) == "is" && $gender_utils:get_conj("says", player) == "says")
              player:tell("PASS: $gender_utils:get_conj() singular");
              passed = passed + 1;
            else
              player:tell("FAIL: $gender_utils:get_conj() singular");
              failed = failed + 1;
            endif

            gender_obj = create($thing);
            gender_obj.gender = "plural";
            if ($gender_utils:get_conj("is/are", gender_obj) == "are" && $gender_utils:get_conj("says", gender_obj) == "say")
              player:tell("PASS: $gender_utils:get_conj() plural");
              passed = passed + 1;
            else
              player:tell("FAIL: $gender_utils:get_conj() plural");
              failed = failed + 1;
            endif

            if ($gender_utils:get_conj("Runs", player) == "Runs" && $gender_utils:get_conj("Runs", gender_obj) == "Run")
              player:tell("PASS: $gender_utils:get_conj() capitalization");
              passed = passed + 1;
            else
              player:tell("FAIL: $gender_utils:get_conj() capitalization");
              failed = failed + 1;
            endif

            if (toobj(34) == #34 && toobj(player) == player && toobj(E_TYPE) == #1)
              player:tell("PASS: toobj() scalar conversions");
              passed = passed + 1;
            else
              player:tell("FAIL: toobj() scalar conversions");
              failed = failed + 1;
            endif

            if (typeof(1) == INT && typeof(player) == OBJ && typeof("x") == STR && typeof(E_PERM) == ERR && typeof({1}) == LIST)
              player:tell("PASS: type constants");
              passed = passed + 1;
            else
              player:tell("FAIL: type constants");
              failed = failed + 1;
            endif

            if (toobj("34") == #34 && toobj("#34") == #34 && toobj("#34.7") == #34 && toobj("wat") == #0)
              player:tell("PASS: toobj() string conversions");
              passed = passed + 1;
            else
              player:tell("FAIL: toobj() string conversions");
              failed = failed + 1;
            endif

            caught = 0;
            try
              toobj({});
            except (E_TYPE)
              caught = 1;
            endtry
            if (caught)
              player:tell("PASS: toobj() E_TYPE");
              passed = passed + 1;
            else
              player:tell("FAIL: toobj() E_TYPE");
              failed = failed + 1;
            endif

            caught = 0;
            try
              toint({});
            except (E_TYPE)
              caught = 1;
            endtry
            if (caught)
              player:tell("PASS: toint() E_TYPE");
              passed = passed + 1;
            else
              player:tell("FAIL: toint() E_TYPE");
              failed = failed + 1;
            endif

            literal = toliteral({1, "two", player, E_PERM});
            if (literal == "{1, \"two\", #2, E_PERM}")
              player:tell("PASS: toliteral()");
              passed = passed + 1;
            else
              player:tell("FAIL: toliteral() -- got ", literal);
              failed = failed + 1;
            endif

            if (typeof(E_PERM) == 3 && toliteral(E_PERM) == "E_PERM")
              player:tell("PASS: error values");
              passed = passed + 1;
            else
              player:tell("FAIL: error values");
              failed = failed + 1;
            endif

            caught = 0;
            try
              toliteral();
            except (E_ARGS)
              caught = 1;
            endtry
            if (caught)
              player:tell("PASS: toliteral() E_ARGS");
              passed = passed + 1;
            else
              player:tell("FAIL: toliteral() E_ARGS");
              failed = failed + 1;
            endif

            result = eval("return 2 + 2;");
            if (result == 4)
              player:tell("PASS: eval() expression");
              passed = passed + 1;
            else
              player:tell("FAIL: eval() expression -- expected 4 got ", result);
              failed = failed + 1;
            endif

            result = eval("x = 3; y = 4; return x + y;");
            if (result == 7)
              player:tell("PASS: eval() statements");
              passed = passed + 1;
            else
              player:tell("FAIL: eval() statements -- expected 7 got ", result);
              failed = failed + 1;
            endif

            result = eval("return this:_test_destructure(\"left\", \"right\");");
            if (result)
              player:tell("PASS: eval() verb call");
              passed = passed + 1;
            else
              player:tell("FAIL: eval() verb call");
              failed = failed + 1;
            endif

            test_obj = create($thing);
            set_name(test_obj, "test object");
            add_alias(test_obj, "test-alias");
            add_verb(test_obj, "ping", "return \"pong\";");

            if (test_obj.name == "test object")
              player:tell("PASS: set_name()");
              passed = passed + 1;
            else
              player:tell("FAIL: set_name()");
              failed = failed + 1;
            endif

            if ("test-alias" in test_obj.aliases)
              player:tell("PASS: add_alias()");
              passed = passed + 1;
            else
              player:tell("FAIL: add_alias()");
              failed = failed + 1;
            endif

            if (test_obj:ping() == "pong")
              player:tell("PASS: add_verb()");
              passed = passed + 1;
            else
              player:tell("FAIL: add_verb()");
              failed = failed + 1;
            endif

            move(test_obj, player);

            if (test_obj.location == player)
              player:tell("PASS: move() changes location");
              passed = passed + 1;
            else
              player:tell("FAIL: move() changes location");
              failed = failed + 1;
            endif

            move(test_obj, here);

            if (test_obj.location == here && test_obj in here.contents)
              player:tell("PASS: move() updates contents view");
              passed = passed + 1;
            else
              player:tell("FAIL: move() updates contents view");
              failed = failed + 1;
            endif

            try
              move(here, test_obj);
              player:tell("FAIL: move() prevents recursive containment");
              failed = failed + 1;
            except (E_RECMOVE)
              player:tell("PASS: move() prevents recursive containment");
              passed = passed + 1;
            endtry

            move(test_obj, $nothing);
            if (test_obj.location == $nothing)
              player:tell("PASS: move() to $nothing");
              passed = passed + 1;
            else
              player:tell("FAIL: move() to $nothing");
              failed = failed + 1;
            endif

            if (is_player(player) && !is_player($room))
              player:tell("PASS: is_player()");
              passed = passed + 1;
            else
              player:tell("FAIL: is_player()");
              failed = failed + 1;
            endif

            ternary = 1 ? "yes" | "no";
            if (ternary == "yes")
              player:tell("PASS: ternary true branch");
              passed = passed + 1;
            else
              player:tell("FAIL: ternary true branch");
              failed = failed + 1;
            endif

            ternary = 0 ? "yes" | "no";
            if (ternary == "no")
              player:tell("PASS: ternary false branch");
              passed = passed + 1;
            else
              player:tell("FAIL: ternary false branch");
              failed = failed + 1;
            endif

            player:tell("---");
            player:tell(passed, " passed, ", failed, " failed.");
        """));
    }
}
