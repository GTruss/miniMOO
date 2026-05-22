using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUnitTests(InMemoryObjectRepository repo) {
        var wiz = repo.Get(WorldIds.Wiz);
        if (wiz is null)
            return;

        wiz.Verbs.Add(ScriptVerb(["_test_destructure"], """
            {a, b} = args;
            return (a == "left" && b == "right");
        """));

        wiz.Verbs.Add(ScriptVerb(["@test"], """
            player:tell("Running miniMOO tests...");

            passed = 0;
            failed = 0;

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

            result = listappend({"a"}, "a");
            if (length(result) == 2 && result[2] == "a")
              player:tell("PASS: listappend()");
              passed = passed + 1;
            else
              player:tell("FAIL: listappend()");
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

            if (tostr("obj=", player) == "obj=#2")
              player:tell("PASS: tostr()");
              passed = passed + 1;
            else
              player:tell("FAIL: tostr()");
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

            player:tell("---");
            player:tell(passed, " passed, ", failed, " failed.");
        """));
    }
}
