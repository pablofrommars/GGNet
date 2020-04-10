namespace GGNet
{
    // Note width are font specific. Run pixelWidthCalculator.html
    public static class TextUtils
    {
        public static double Width(this string text, Size size)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0.0;
            }

            var width = 0.0;

            foreach (var c in text)
            {
                //pixelWidthCalculator.html
                width += c switch
                {
                    ' ' => 8, //normally 4 but increased to correct "potential" long text
                    '!' => 5,
                    '\"' => 7,
                    '#' => 8,
                    '$' => 8,
                    '%' => 13,
                    '&' => 12,
                    '\'' => 3,
                    '(' => 5,
                    ')' => 5,
                    '*' => 8,
                    '+' => 9,
                    ',' => 4,
                    '-' => 5,
                    '.' => 4,
                    '/' => 4,
                    '0' => 8,
                    '1' => 8,
                    '2' => 8,
                    '3' => 8,
                    '4' => 8,
                    '5' => 8,
                    '6' => 8,
                    '7' => 8,
                    '8' => 8,
                    '9' => 8,
                    ':' => 4,
                    ';' => 4,
                    '<' => 9,
                    '=' => 9,
                    '>' => 9,
                    '?' => 7,
                    '@' => 15,
                    'A' => 12,
                    'B' => 11,
                    'C' => 11,
                    'D' => 12,
                    'E' => 10,
                    'F' => 9,
                    'G' => 12,
                    'H' => 12,
                    'I' => 5,
                    'J' => 6,
                    'K' => 12,
                    'L' => 10,
                    'M' => 14,
                    'N' => 12,
                    'O' => 12,
                    'P' => 9,
                    'Q' => 12,
                    'R' => 11,
                    'S' => 9,
                    'T' => 10,
                    'U' => 12,
                    'V' => 12,
                    'W' => 15,
                    'X' => 12,
                    'Y' => 12,
                    'Z' => 10,
                    '[' => 5,
                    '\\' => 4,
                    ']' => 5,
                    '^' => 8,
                    '_' => 8,
                    '`' => 5,
                    'a' => 7,
                    'b' => 8,
                    'c' => 7,
                    'd' => 8,
                    'e' => 7,
                    'f' => 5,
                    'g' => 8,
                    'h' => 8,
                    'i' => 4,
                    'j' => 4,
                    'k' => 8,
                    'l' => 4,
                    'm' => 12,
                    'n' => 8,
                    'o' => 8,
                    'p' => 8,
                    'q' => 8,
                    'r' => 5,
                    's' => 6,
                    't' => 4,
                    'u' => 8,
                    'v' => 8,
                    'w' => 12,
                    'x' => 8,
                    'y' => 8,
                    'z' => 7,
                    '{' => 8,
                    '|' => 3,
                    '}' => 8,
                    '~' => 9,
                    _ => 4
                };
            }

            var relative = size.Value;

            if (size.Units == Units.px)
            {
                relative /= 16;
            }

            return relative * width;
        }

        public static double Height(this Size size) => size.Value * (size.Units == Units.em ? 16 : 1);

        public static double Height(this string text, Size size) => string.IsNullOrEmpty(text) ? 0 : size.Height();
    }
}
