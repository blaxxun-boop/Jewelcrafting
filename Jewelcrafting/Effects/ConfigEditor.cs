using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Cursor = UnityEngine.Cursor;

namespace Jewelcrafting;

public partial class Jewelcrafting
{
	private const int WindowId = -669;
	private Rect yamlWindowRect;
	private Dictionary<string, string> currentYamlInput = null!;
	private readonly Dictionary<string, bool> collapsed = new();
	private readonly Dictionary<string, Vector2> yamlTextareaScrollPosition = new();
	private Vector2 yamlErrorsScrollPosition;
	private bool hasErrors = false;

	private CustomSyncedValue<List<string>>? activeConfigFileData = null;
	private Func<object?, List<string>> configChecker = null!;

	private void DrawYamlEditorButton(ConfigEntryBase _)
	{
		GUILayout.BeginVertical();

		foreach (ConfigLoader.Loader loader in ConfigLoader.loaders)
		{
			if (loader.FileData.Value.Count > 0 && GUILayout.Button(loader.EditButtonName, GUILayout.ExpandWidth(true)))
			{
				activeConfigFileData = loader.FileData;
				configChecker = loader.ErrorCheck;

				currentYamlInput = activeConfigFileData.Value.Select((f, index) => new { f, index }).GroupBy(g => g.index / 2).ToDictionary(g => g.First().f, g => g.Last().f);
				foreach (string file in currentYamlInput.Keys)
				{
					if (!collapsed.ContainsKey(file))
					{
						collapsed[file] = collapsed.Count > 3;
						yamlTextareaScrollPosition[file] = new Vector2();
					}
				}
			}
		}

		GUILayout.EndVertical();
	}

	private void Update()
	{
		if (activeConfigFileData != null)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	private void LateUpdate() => Update();

	private void OnGUI()
	{
		if (activeConfigFileData == null)
		{
			return;
		}

		Update();

		yamlWindowRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
		GUILayout.Window(WindowId, yamlWindowRect, yamlWindowDrawer, $"{ModName} YAML Editor");
	}

	private void yamlWindowDrawer(int id)
	{
		GUI.Box(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
		GUILayout.BeginHorizontal();
		GUI.enabled = !hasErrors && (!configSync.IsLocked || configSync.IsSourceOfTruth);

		void save() => activeConfigFileData!.Value = currentYamlInput.SelectMany(kv => new[] { kv.Key, kv.Value }).ToList();
		if (GUILayout.Button(Localization.instance.Localize("$jc_config_editor_save")) && !hasErrors)
		{
			save();
			activeConfigFileData = null;
		}

		if (configSync.IsSourceOfTruth && GUILayout.Button(Localization.instance.Localize("$jc_config_editor_apply")) && !hasErrors)
		{
			ConfigLoader.SkipSavingOfValueChange = true;
			save();
			ConfigLoader.SkipSavingOfValueChange = false;
			activeConfigFileData = null;
		}

		GUI.enabled = true;
		if (GUILayout.Button(Localization.instance.Localize("$jc_config_editor_discard")))
		{
			activeConfigFileData = null;
		}

		GUILayout.EndHorizontal();

		hasErrors = false;
		string yamlErrorContent = "";
		foreach (string file in currentYamlInput.Keys.ToList())
		{
			if (GUI.GetNameOfFocusedControl() == $"Jewelcrafting yaml textarea for {file}" && Event.current.type is EventType.KeyDown or EventType.KeyUp && Event.current.isKey)
			{
				if (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')
				{
					if (Event.current.type == EventType.KeyUp)
					{
						TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
						editor.Insert(' ');
						editor.Insert(' ');
						currentYamlInput[file] = editor.text;
					}

					Event.current.Use();
				}

				// repeat indent of previous line on enter
				if (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return || Event.current.character == '\n')
				{
					if (Event.current.type == EventType.KeyUp)
					{
						TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
						string text = editor.text;
						int lineStartIndex = editor.cursorIndex;
						if (lineStartIndex > 0)
						{
							do
							{
								if (text[lineStartIndex - 1] == '\n')
								{
									break;
								}
							} while (--lineStartIndex > 0);
						}

						int lastSpaceIndex = lineStartIndex;
						if (lastSpaceIndex < text.Length && text[lastSpaceIndex] == ' ')
						{
							while (true)
							{
								if (lastSpaceIndex >= text.Length || text[lastSpaceIndex] == '\n')
								{
									lastSpaceIndex = lineStartIndex;
									break;
								}

								if (text[lastSpaceIndex] != ' ')
								{
									break;
								}

								++lastSpaceIndex;
							}
						}

						if (lastSpaceIndex > editor.cursorIndex)
						{
							lastSpaceIndex = lineStartIndex;
						}

						editor.Insert('\n');
						for (int i = lastSpaceIndex - lineStartIndex; i > 0; --i)
						{
							editor.Insert(' ');
						}

						currentYamlInput[file] = editor.text;
					}

					Event.current.Use();
				}
			}

			if (currentYamlInput.Count > 1)
			{
				GUILayout.BeginVertical(GUI.skin.box);
				if (GUILayout.Button(Path.GetFileName(file), new GUIStyle(GUI.skin.label)
				    {
					    alignment = TextAnchor.UpperCenter,
					    wordWrap = true,
					    stretchWidth = true,
					    fontSize = 15,
				    }, GUILayout.ExpandWidth(true)))
				{
					collapsed[file] = !collapsed[file];
				}
			}
			else
			{
				collapsed[file] = false;
			}

			if (!collapsed[file])
			{
				yamlTextareaScrollPosition[file] = GUILayout.BeginScrollView(yamlTextareaScrollPosition[file], GUILayout.ExpandHeight(true));
				GUI.SetNextControlName($"Jewelcrafting yaml textarea for {file}");
				currentYamlInput[file] = GUILayout.TextArea(currentYamlInput[file], GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				GUILayout.EndScrollView();
			}

			if (currentYamlInput.Count > 1)
			{
				GUILayout.EndVertical();
			}

			try
			{
				object configObj = new DeserializerBuilder().Build().Deserialize<object>(currentYamlInput[file]);
				List<string> errors = configChecker(configObj);
				if (errors.Count > 0)
				{
					hasErrors = true;
					yamlErrorContent = $"{yamlErrorContent}There are errors in your {Path.GetFileName(file)} config:\n{string.Join("\n", errors)}\n";
				}
			}
			catch (YamlException e)
			{
				hasErrors = true;
				yamlErrorContent = $"{yamlErrorContent}The syntax of your {Path.GetFileName(file)} config is invalid, parsing failed:\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}\n";
			}
		}

		GUILayout.BeginVertical(GUI.skin.box);
		yamlErrorsScrollPosition = GUILayout.BeginScrollView(yamlErrorsScrollPosition, GUILayout.Height(100));

		if (yamlErrorContent != "")
		{
			GUIStyle labelStyle = new(GUI.skin.label)
			{
				normal =
				{
					textColor = new Color(200, 50, 50),
				},
			};
			Color oldColor = GUI.contentColor;
			GUI.contentColor = new Color(200, 50, 50);
			GUILayout.Label(yamlErrorContent, labelStyle);
			GUI.contentColor = oldColor;
		}
		else
		{
			GUILayout.Label("Configuration syntax is valid.");
		}

		GUILayout.EndScrollView();
		GUILayout.EndVertical();
	}
}