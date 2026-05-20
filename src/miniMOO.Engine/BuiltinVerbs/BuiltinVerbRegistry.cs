using System;
using System.Collections.Generic;
using System.Text;

namespace miniMOO.Engine.BuiltinVerbs;

public sealed class BuiltinVerbRegistry {
    private readonly Dictionary<string, IBuiltinVerb> _verbs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IBuiltinVerb verb)
        => _verbs.Add(verb.Name, verb);

    public IBuiltinVerb? Find(string name)
        => _verbs.GetValueOrDefault(name);
}
