#if PEGGLE_IPHONE
namespace Sexy
{
    /// <summary>
    /// iPhone: HD resource names for images the iPhone build keeps under another id. Each alias is
    /// the image the iPhone code uses in the same place (checked against its decompile).
    /// </summary>
    public static partial class Res
    {
        public static Image IMAGE_MM_BUTTON_BLUE => IMAGE_DLG_BUTTON_BLUE;

        public static Image IMAGE_MM_NIGHTS_BUTTON_PURPLE => IMAGE_DLG_BUTTON_PURPLE;

        public static Image IMAGE_BUTTON_ORANGE => IMAGE_DLG_BUTTON_ORANGE;

        public static Image IMAGE_HU_LARGE => IMAGE_CHARSEL_10_HU_LARGE;

        public static Image IMAGE_WAREN_LARGE => IMAGE_CHARSEL_08_WAREN_LARGE;

        public static Image IMAGE_MM_APPROVEDSTAMP => IMAGE_APPROVEDSTAMP;

        public static Image IMAGE_MM_NIGHTS_APPROVEDSTAMP => IMAGE_APPROVEDSTAMP;

        public static Image IMAGE_LS_LOCK_SMALL => IMAGE_LOCK_SMALL;
    }
}
#endif
