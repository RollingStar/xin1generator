using System;

namespace Xin1Generator {
    [Serializable]
    public class ParameterException : Exception {
        public ParameterException(string message) : base(message) { }
    }
}
