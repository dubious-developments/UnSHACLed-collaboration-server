namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// Defines helpers that make it easy to compose very basic HTML pages.
    /// </summary>
    public static class HtmlHelpers
    {
        /// <summary>
        /// Creates a very simple HTML page from a title
        /// and a body. Title and body are not escaped, so
        /// be careful.
        /// </summary>
        /// <param name="title">The page's title.</param>
        /// <param name="body">The page's contents.</param>
        /// <returns>Source for an HTML page.</returns>
        public static string CreateHtmlPage(string title, string body)
        {
            string template = @"
<html>
<head>
<meta charset=""utf-8"" />
<link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"">
<title>{0}</title>
</head>

<body>
{1}
</body>

</html>";

            return string.Format(template, title, body);
        }
    }
}