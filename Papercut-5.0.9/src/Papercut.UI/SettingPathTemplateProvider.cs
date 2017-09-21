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

namespace Papercut
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Paths;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class SettingPathTemplateProvider : IPathTemplatesProvider,
        IEventHandler<SettingsUpdatedEvent>
    {
        readonly ILogger _logger;

        public SettingPathTemplateProvider(ILogger logger)
        {
            _logger = logger;
            PathTemplates = new ObservableCollection<string>(MessagePaths);
        }

        IEnumerable<string> MessagePaths
        {
            get
            {
                return
                    Settings.Default.MessagePaths.Split(new[] { ';', ',' })
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct();
            }
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            UpdatePathTemplates();
        }

        public ObservableCollection<string> PathTemplates { get; private set; }

        void UpdatePathTemplates()
        {
            string[] paths = MessagePaths.ToArray();
            List<string> toRemove = PathTemplates.Except(paths).ToList();
            List<string> toAdd = paths.Except(PathTemplates).ToList();

            if (toRemove.Any()) _logger.Information("Removing Path(s) {Paths} to Path Templates", toRemove);

            toRemove.ForEach(s => PathTemplates.Remove(s));

            if (toAdd.Any()) _logger.Information("Added Path(s) {Paths} to Path Templates", toAdd);

            PathTemplates.AddRange(toAdd);
        }
    }
}