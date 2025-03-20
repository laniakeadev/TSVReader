using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class TSVReader
{
    private const bool AvoidSplittingInQuotationMarks = false;
    private readonly string[] lineSeparator = new string[] { "\r\n", "\n\r", "\n" };
    private readonly string folder = "Localization";
    private readonly char separator = '\t';

    private Regex valueParser;

    public TSVReader()
    {
        valueParser = AvoidSplittingInQuotationMarks ? new Regex("\t(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))") : new Regex("\t");
    }

    public async UniTask Read(string language, string filename, Dictionary<string, string> dictionary)
    {
        var tsvFile = await LoadFromResourcesAsync(filename);
        var lines = tsvFile.text.Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries);
        var headers = lines[0].Split(separator, StringSplitOptions.None);

        int targetColumn = ReadHeader(language, headers);

        ParseValues(lines, targetColumn, dictionary, filename);

        LogLoadingTime(filename);
    }

    private int ReadHeader(string language, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            if (headers[i].Contains(language))
            {
                return i;
            }
        }

        throw new Exception("Header with required language is not found");
    }

    private async UniTask<TextAsset> LoadFromResourcesAsync(string filename)
    {
        var request = Resources.LoadAsync($"{folder}/{filename}", typeof(TextAsset));
        await request.ToUniTask();
        return request.asset as TextAsset;
    }

    private TextAsset LoadFromResources(string filename)
    {
        return Resources.Load<TextAsset>($"{folder}/{filename}");
    }

    private async UniTask<TextAsset> LoadFromAddressablesAsync(string filename)
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>($"Assets/{folder}/{filename}.tsv");
        await handle.Task;
        return handle.Result;
    }

    private void ParseValues(string[] lines, int languageId, Dictionary<string, string> dictionary, string filename)
    {
        for (var i = 1; i < lines.Length; i++) // ignore key (i=0)
        {
            var fields = valueParser.Split(lines[i]);

            var key = fields[0];
            if (key == String.Empty) continue;
            if (dictionary.ContainsKey(key))
            {
                Debug.LogError("String duplicate found: " + key);
                continue;
            }

            string value;
            try
            {
                value = fields[languageId];
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError("IndexOutOfRangeException: Check out file formatting! " + filename);
                break;
            }

            dictionary.Add(key, value);
        }
    }

    private void LogLoadingTime(string filename)
    {
        Debug.Log($"{Time.realtimeSinceStartup} parsing finished {filename}");
    }
}
