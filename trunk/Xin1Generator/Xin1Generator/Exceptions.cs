using System;

namespace Xin1Generator {
    [Serializable]
    class ParameterException : Exception {
        public ParameterException(string message) : base(message) { }
    }
}
