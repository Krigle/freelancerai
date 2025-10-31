using System;
using System.Text.RegularExpressions;

namespace FreelanceFinderAI.Services
{
    public class JobTextPreprocessor : IJobTextPreprocessor
    {
        public string RemoveHtmlEntities(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Decode HTML entities
            return System.Net.WebUtility.HtmlDecode(input);
        }

        public string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Replace multiple whitespace characters with single space
            return Regex.Replace(input, @"\s+", " ").Trim();
        }

        public string RemoveWebpageNoise(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove common webpage noise patterns
            string result = input;

            // Remove script tags and their content
            result = Regex.Replace(result, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);

            // Remove style tags and their content
            result = Regex.Replace(result, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            // Remove HTML comments
            result = Regex.Replace(result, @"<!--[\s\S]*?-->", "");

            // Remove navigation elements (basic pattern)
            result = Regex.Replace(result, @"<nav[^>]*>[\s\S]*?</nav>", "", RegexOptions.IgnoreCase);

            // Remove footer elements
            result = Regex.Replace(result, @"<footer[^>]*>[\s\S]*?</footer>", "", RegexOptions.IgnoreCase);

            // Remove header elements (keeping some content)
            result = Regex.Replace(result, @"<header[^>]*>[\s\S]*?</header>", "", RegexOptions.IgnoreCase);

            // Remove common advertisement patterns
            result = Regex.Replace(result, @"<div[^>]*class\s*=\s*['""]\s*ad[s]?\s*['""][^>]*>[\s\S]*?</div>", "", RegexOptions.IgnoreCase);

            // Remove excessive line breaks and normalize
            result = Regex.Replace(result, @"(\r?\n\s*){3,}", "\n\n");

            return result.Trim();
        }

        public bool IsValidInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Check minimum length (reasonable for job descriptions)
            if (input.Length < 10)
                return false;

            // Check for excessive special characters (potential spam)
            int specialCharCount = Regex.Matches(input, @"[^\w\s]").Count;
            if (specialCharCount > input.Length * 0.5)
                return false;

            // Check for reasonable word count
            string[] words = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 5)
                return false;

            return true;
        }
    }
}