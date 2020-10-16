﻿using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Models.Variables
{
    public class DictionaryOfStringsVariable : Variable
    {
        private Dictionary<string, string> value = new Dictionary<string, string>();

        public DictionaryOfStringsVariable(Dictionary<string, string> value)
        {
            this.value = value;
            Type = VariableType.DictionaryOfStrings;
        }

        public override string AsString() => 
            "{" + string.Join(", ", AsListOfStrings().Select(s => $"({s})")) + "}";

        public override List<string> AsListOfStrings() =>
            value.Select(kvp => $"{kvp.Key}, {kvp.Value}").ToList();

        public override Dictionary<string, string> AsDictionaryOfStrings()
            => value;
    }
}