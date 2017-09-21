﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Papercut.Rules
{
    using System;
    using System.Collections.Generic;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Rules;
    using Papercut.Core.Infrastructure.Json;

    public class RuleRespository
    {
        public void SaveRules([NotNull] IList<IRule> rules, string path)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (path == null) throw new ArgumentNullException(nameof(path));

            JsonHelpers.SaveJson(rules, path);
        }

        public IList<IRule> LoadRules([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return JsonHelpers.LoadJson<IList<IRule>>(path, () => new List<IRule>(0));
        }
    }
}