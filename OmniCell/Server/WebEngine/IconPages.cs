namespace WebEngine
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// The icon browser: a page listing every item icon in the client, searchable by item name,
    /// item ID and icon ID, and the endpoints it loads from.
    /// </summary>
    public static class IconPages
    {
        public const string Page = "/icons";

        public const string Status = "/icons/status";

        public const string CatalogJson = "/icons/catalog.json";

        /// <summary>Followed by the icon ID and ".png".</summary>
        public const string ImagePrefix = "/icons/";

        private const string Json = "application/json; charset=utf-8";

        private const int ImageCacheSeconds = 86400;

        private static readonly Lazy<byte[]> PageHtml = new Lazy<byte[]>(LoadPage);

        public static PageResult Route(HttpRequest request)
        {
            string p = "/" + request.Path.Trim('/').ToLowerInvariant();
            if (p != Page && !p.StartsWith(ImagePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            // Everything under /icons is for signed-in game admins.
            if (AdminAuth.Current(request) == null)
            {
                return AdminAuth.Challenge(request, p == Page);
            }

            if (p == Page)
            {
                IconCatalog.StartLoading();
                return new PageResult(200, PageHtml.Value, "text/html; charset=utf-8", 0, null);
            }

            if (p == Status)
            {
                IconCatalog.StartLoading();
                return new PageResult(200, IconCatalog.StatusJson(), Json, 0, null);
            }

            if (p == CatalogJson)
            {
                IconCatalog.Catalog catalog = IconCatalog.Ready;
                return catalog == null
                           ? new PageResult(503, Encoding.UTF8.GetBytes("{\"error\":\"still loading\"}"), Json, 0, null)
                           : new PageResult(200, catalog.Json, Json, 0, catalog.Gzip);
            }

            if (p.StartsWith(ImagePrefix, StringComparison.Ordinal) && p.EndsWith(".png", StringComparison.Ordinal))
            {
                string digits = p.Substring(ImagePrefix.Length, p.Length - ImagePrefix.Length - 4);
                int id;
                if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    return null;
                }

                byte[] image = IconCatalog.Image(id);
                if (image == null)
                {
                    int status = IconCatalog.Ready == null ? 503 : 404;
                    return new PageResult(status, Encoding.UTF8.GetBytes("No icon " + id), "text/plain; charset=utf-8", 0, null);
                }

                // One icon record is a JPEG; the rest are PNGs.
                string type = image.Length > 1 && image[0] == 0xFF && image[1] == 0xD8 ? "image/jpeg" : "image/png";
                return new PageResult(200, image, type, ImageCacheSeconds, null);
            }

            return null;
        }

        private static byte[] LoadPage()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WebEngine.Assets.icons.html"))
            using (MemoryStream buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}
