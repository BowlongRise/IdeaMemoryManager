namespace IdeaMemoryManager.Common
{
    public static class ByteSizeFormatter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        public static string Format(long bytes)
        {
            if (bytes <= 0) return "0.0 MB";

            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.00} {Units[unitIndex]}";
        }

        public static string FormatMb(long bytes)
        {
            double mb = bytes / (1024.0 * 1024.0);
            return $"{mb:0.0} MB";
        }

        public static string FormatGb(long bytes)
        {
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
            return $"{gb:0.00} GB";
        }
    }
}