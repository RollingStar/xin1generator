using System;
using System.Diagnostics;
using System.Windows.Controls;

namespace Xin1GeneratorGUI {
    class TextBlockTraceListener : TraceListener {
        TextBlock textBlock;
        string parentMessage = string.Empty;

        public TextBlockTraceListener(TextBlock textBlock) {
            this.textBlock = textBlock;
        }

        public override void Write(string message) {
            if (message.Trim().Length != 0)
                WriteTextBlockText(message);
        }

        public override void WriteLine(string message) {
            if (IndentLevel == 0)
                parentMessage = message;

            WriteTextBlockText(message);
        }

        private void WriteTextBlockText(string message) {
            textBlock.Dispatcher.Invoke(new Action(() => {
                textBlock.Text =
                    (IndentLevel > 0 ? parentMessage + " " : string.Empty) + message;
            }));
        }
    }
}
