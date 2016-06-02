﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>
    /// Provides a means of interacting with the active editor window in the Visual Studio host.
    /// </summary>
    public partial class EditorWindow
    {
        private readonly VisualStudioInstance _visualStudioInstance;
        private readonly EditorWindowWrapper _editorWindowWrapper;

        internal EditorWindow(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;

            // Create MarshalByRefObject that can be used to execute code in the VS process.
            _editorWindowWrapper = _visualStudioInstance.ExecuteInHostProcess<EditorWindowWrapper>(
                type: typeof(EditorWindowWrapper),
                methodName: nameof(EditorWindowWrapper.Create));
        }

        public string GetText() => _editorWindowWrapper.GetText();
        public void SetText(string value) => _editorWindowWrapper.SetText(value);

        public string GetCurrentLineText() => _editorWindowWrapper.GetCurrentLineText();
        public int GetCaretPosition() => _editorWindowWrapper.GetCaretPosition();
        public string GetLineTextBeforeCaret() => _editorWindowWrapper.GetLineTextBeforeCaret();
        public string GetLineTextAfterCaret() => _editorWindowWrapper.GetLineTextAfterCaret();

        public void MoveCaret(int position) => _editorWindowWrapper.MoveCaret(position);

        public void Activate()
        {
            IntegrationHelper.RetryRpcCall(() =>
            {
                _visualStudioInstance.DTE.ActiveDocument.Activate();
            });
        }
    }
}
