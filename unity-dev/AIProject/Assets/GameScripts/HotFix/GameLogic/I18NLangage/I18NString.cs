
#nullable enable

namespace GameLogic {
    public class I18NString {
        private readonly string _i18Id;
        public I18NString (string i18Id) {
            _i18Id = i18Id;
        }

        public override string ToString ()
        {
            return I18N.GetString(_i18Id);
        }

        public static implicit operator System.String(I18NString i18NString) {
            return i18NString.ToString();
        }
    }
}