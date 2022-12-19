﻿using DiffMatchPatch;
using Ganss.XSS;
using Shaman.Runtime;

namespace Typerary.Shared
{

    public class PracticeSectionResult
    {
        private static diff_match_patch dmp;
        private readonly Func<ValueString, string> insertHtmlStyle = (ValueString text) => $"<ins style=\"background:#e6ffe6; font-weight:bold\">{text}</ins>";
        private readonly Func<ValueString, string> deleteHtmlStyle = (ValueString text) => $"<del style=\"background:#ffe6e6; font-weight:bold\">{text}</del>";
        private readonly Func<ValueString, ValueString, string> replaceHtmlStyle = (ValueString wrongText, ValueString collectText) => $"<span style=\"background:#e6e6ff; font-weight:bold\"><del>{wrongText}</del> &rarr; {collectText}</span>";
        private readonly Func<ValueString, string> equalHtmlStyle = (ValueString text) => $"<span style=\"color:#666666\">{text}</span>";
        public int CollectCount { get; private set; } = 10;
        public int WrongCount { get; private set; } = 1;

        public string JudgeSentence { init; get; }
        public string InputSentence { init; get; }
        public string DiffMarkUpSentence { get; private set; }

        public PracticeSectionResult(string judgeSentence, string inputSentence)
        {
            dmp ??= new();
            JudgeSentence = judgeSentence;
            InputSentence = inputSentence;
        }

        private List<Diff> GenerateDiff() => dmp.diff_main(InputSentence, JudgeSentence);

        private string ConvertDiffsToHtmlString(List<Diff> diffs)
        {
            var stringBuilder = ReseekableStringBuilder.AcquirePooledStringBuilder();
            for (var i = 0; i < diffs.Count; i++)
            {
                var ithOperation = diffs[i].operation;
                var ithText = diffs[i].text;
                var isReplace = (i + 1 < diffs.Count) && (diffs[i + 1].operation is Operation.INSERT);
                stringBuilder.Append(
                    ithOperation switch
                    {
                        Operation.EQUAL => equalHtmlStyle.Invoke(ithText),
                        Operation.INSERT => insertHtmlStyle.Invoke(ithText),
                        Operation.DELETE when isReplace => replaceHtmlStyle.Invoke(ithText, diffs[i + 1].text),
                        Operation.DELETE => deleteHtmlStyle.Invoke(ithText),
                        _ => ""
                    });
                if (isReplace)
                {
                    i++;
                }
            }

            var text = stringBuilder.ToString();
            var sanitizer = new HtmlSanitizer();
            var ret = sanitizer.Sanitize(text);
            ReseekableStringBuilder.Release(stringBuilder);
            return ret;
        }

        public void SetDiffMarkUpSentence()
        {
            var diffs = GenerateDiff();
            foreach (var diff in diffs)
            {
                Console.WriteLine($"({diff.operation}, {diff.text}");
            }
            DiffMarkUpSentence = ConvertDiffsToHtmlString(diffs);
        }
    }
}
