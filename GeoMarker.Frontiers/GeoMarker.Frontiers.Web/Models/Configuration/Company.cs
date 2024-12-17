namespace GeoMarker.Frontiers.Web.Models.Configuration
{
    public static class Company
    {
        public static string? Name { get; set; } = string.Empty;
        public static string? LogoURL { get; set; } = string.Empty;
        public static string? LogoHeight { get; set; } = string.Empty;
        public static string? LogoWidth { get; set; } = string.Empty;
        public static string? FaviconUrl { get; set; } = string.Empty;
        public static Position LogoPosition { get; set; } = Position.TopLeft;
        public static string? SupportContactInformation { get; set; } = string.Empty;
        public static string GetTopLogoImg(Position pos)
        {
            if (!string.IsNullOrEmpty(LogoURL) && LogoPosition == pos)
            {
                string topCenter = pos == Position.TopCenter ? "mx-auto" : "";
                return GetLogoImg(topCenter);
            }
            return "";
        }

        public static string GetBottomLogoImg()
        {
            if (BottomPosition())
                return GetLogoImg("", "style=\"margin-bottom: 15px;\"");
            return string.Empty;
        }

        private static string GetLogoImg(string topCenter, string bottom = "")
        {
            if (string.IsNullOrEmpty(LogoURL))
                return string.Empty;
            string img = $"<a class=\"navbar-brand {topCenter}\"><img src={LogoURL} {bottom}";
            if (!string.IsNullOrEmpty(LogoHeight) && !string.IsNullOrEmpty(LogoWidth))
                return $"{img} height={LogoHeight} width={LogoWidth}></a>";
            return img + "/></a>";
        }

        public static string GetFooterStyle()
        {
            switch (LogoPosition)
            {
                case Position.BottomLeft: return string.Empty;
                case Position.BottomRight: return "text-right";
                default: return "text-center";
            }
        }

        public static bool BottomPosition()
        {
            return LogoPosition == Position.BottomLeft ||
                LogoPosition == Position.BottomCenter ||
                LogoPosition == Position.BottomRight;
        }

        public enum Position
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

    }
}
