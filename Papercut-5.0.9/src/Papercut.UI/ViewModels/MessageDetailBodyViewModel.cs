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

namespace Papercut.ViewModels
{
    using System;

    using Caliburn.Micro;

    using ICSharpCode.AvalonEdit.Document;

    using Papercut.Helpers;
    using Papercut.Views;

    using Serilog;

    public class MessageDetailBodyViewModel : Screen, IMessageDetailItem
    {
        string _body;

        readonly ILogger _logger;

        public MessageDetailBodyViewModel(ILogger logger)
        {
            _logger = logger;
            DisplayName = "Body";
        }

        public string Body
        {
            get { return _body; }
            set
            {
                _body = value;
                NotifyOfPropertyChange(() => Body);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MessageDetailBodyView;

            if (typedView == null)
            {
                _logger.Error("Unable to locate the MessageDetailBodyView to hook the Text Control");
                return;
            }

            this.GetPropertyValues(p => p.Body)
                .Subscribe(
                    t => { typedView.BodyEdit.Document = new TextDocument(new StringTextSource(t ?? string.Empty)); });
        }
    }
}