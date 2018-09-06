//======================================================
//从文件中读取数据
//[1]
//prefab = handsome
//scale=1
//facecameraoffset=-0.11,-1.9,1.3
//[11]
//prefab=bird
//scale = 1
//facecameraoffset=-0.11,-1.9,1.3
//[51]
//prefab=m0001
//scale = 1
//facecameraoffset=-0.11,-1.9,1.3
//并将其存在PlayerDataTableManager.playerDataMap中
//======================================================
/// <summary>
/// 数据类
/// </summary>
public class PlayerTableData
{
	public string playfabName = string.Empty;
	public float scale = 1f;
	public Vector3 faceCameraOffset = Vector3.zero;
}
/// <summary>
/// 玩家数据管理
/// </summary>
public static class PlayerDataTableManager
{
	public static Dictionary<string, PlayerTableData> playerDataMap = new Dictionary<string, PlayerTableData>();
	public static void LoadFromFile(string path)
	{
		string fileContent = "";
		using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using (StreamReader sr = new StreamReader(fs))
			{
				string text = sr.ReadToEnd();
				if (text.Length > 0)
				{
					fileContent = text;
				}
			}
		}

		int lineIndex = 0;
		FileTextParser parser = new FileTextParser();
		parser.Init(fileContent);
		string keyStr = "";
		while (!parser.IsEOF())
		{
			++lineIndex;
			string line = parser.ReadLine().Trim();
			if (string.IsNullOrEmpty(line))
				continue;

			if (line[0] == '[')
			{
				bool bFindChar = false;
				keyStr = "";
				for (int i = 0; i < line.Length; ++i)
				{
					if (line[i] == '[' || line[i] == ']')
					{
						bFindChar = true;

						if (line[i] == ']') // end?
						{
							bFindChar = false;
						}
						else
						{
							continue;
						}
					}
					else if (bFindChar)
					{
						keyStr += line[i];
					}
				}

				continue;
			}
			if (!playerDataMap.ContainsKey(keyStr))
			{
				playerDataMap[keyStr] = new PlayerTableData();
			}


			int bodyIndex = line.IndexOf('=');
			if (bodyIndex <= 0)
			{
				string msg = string.Format("Text body not exist (line:{0}/{1})", lineIndex, path);
				throw new System.Exception(msg);
			}

			if (bodyIndex > 0)
			{
				string key = line.Substring(0, bodyIndex).Trim();
				string body = line.Substring(bodyIndex + 1).Trim(' ', '\t').Replace("\\n", "\n");


				switch (key)
				{
					case "prefab":
						playerDataMap[keyStr].playfabName = body;
						break;
					case "scale":
						playerDataMap[keyStr].scale = float.Parse(body);
						break;
					case "facecameraoffset":
						ParseVector3(body, ref playerDataMap[keyStr].faceCameraOffset, ',');
						break;
					default:
						break;
				}
			}

		}

	}
	public static bool ParseVector3(string content, ref Vector3 output, char split = ',')
	{
		string[] values = content.Split(split);
		if (null == values || values.Length < 3)
			return false;

		try
		{
			output.x = float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
			output.y = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
			output.z = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);
		}
		catch (System.Exception e)
		{
			Debug.LogError(e);
			throw e;
		}
		return true;
	}
}

/// <summary>
/// 通用的文本解释器
/// </summary>
public class FileTextParser
{
	private string _fileText = null;
	private int _currIndex = -1;
	private int _currLine = -1;

	private int _cutoffLength = 0;

	public void Clear()
	{
		_fileText = null;
		_currIndex = -1;
		_currLine = -1;
		_cutoffLength = 0;
	}

	public void Init(string fileText)
	{
		_fileText = fileText;
		_currIndex = 0;
		_currLine = 0;


		if (_fileText.IndexOf('\r') >= 0)
		{
			_cutoffLength = 1;
		}
		else
		{
			_cutoffLength = 0;
		}
	}
	/// <summary>
	/// 每次读取一行
	/// </summary>
	/// <returns></returns>
	public string ReadLine()
	{
		if (_currIndex < 0)
			return null;

		int nextIndex = _fileText.IndexOf('\n', _currIndex);
		if (nextIndex > 0)
		{
			int length = nextIndex - _currIndex - _cutoffLength;
			if (length < 0)
			{
				Debug.LogError("[FileTextParser]ReadLine Cannot be negative : " + length);
			}
			string line = _fileText.Substring(_currIndex, length);
			_currIndex = nextIndex + 1;
			++_currLine;
			return line;
		}
		else if (nextIndex < 0)
		{
			int length = _fileText.Length - _currIndex;
			if (length < 0)
			{
				Debug.LogError("[FileTextParser]ReadLine Cannot be negative : " + length);
			}
			string line = _fileText.Substring(_currIndex, length);
			_currIndex = -1;
			return line;
		}
		else
		{
			_currIndex = -1;
			return null;
		}
	}

	public int GetCurrLine()
	{
		return _currLine;
	}

	public bool IsEOF()
	{
		if (_currIndex < 0 || null == _fileText || _currIndex >= _fileText.Length)
			return true;
		return false;
	}
}