﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class StyleModule : Module {

	public bool[] Keys = new bool[0];
	public StyleFunction[] Functions = new StyleFunction[0];

	public void Repair() {
		if(Keys.Length != Data.GetTotalFrames()) {
			Keys = new bool[Data.GetTotalFrames()];
			Keys[0] = true;
			Keys[Keys.Length-1] = true;
			for(int i=1; i<Keys.Length-1; i++) {
				for(int j=0; j<Functions.Length; j++) {
					if(Functions[j].Values[i] == 0f && Functions[j].Values[i+1] != 0f) {
						Keys[i] = true;
					}
					if(Functions[j].Values[i] == 1f && Functions[j].Values[i+1] != 1f) {
						Keys[i] = true;
					}
					if(Functions[j].Values[i] != 0f && Functions[j].Values[i+1] == 0f) {
						Keys[i+1] = true;
					}
					if(Functions[j].Values[i] != 1f && Functions[j].Values[i+1] == 1f) {
						Keys[i+1] = true;
					}
				}
			}
		}
	}

	public override TYPE Type() {
		return TYPE.Style;
	}

	public override Module Initialise(MotionData data) {
		Data = data;
		Inspect = true;
		Functions = new StyleFunction[0];
		Keys = new bool[data.GetTotalFrames()];
		Keys[0] = true;
		Keys[Keys.Length-1] = true;
		return this;
	}

	public void AddStyle(string name) {
		if(System.Array.Exists(Functions, x => x.Name == name)) {
			Debug.Log("Style with name " + name + " already exists.");
			return;
		}
		ArrayExtensions.Add(ref Functions, new StyleFunction(this, name));
	}

	public void RemoveStyle() {
		ArrayExtensions.Shrink(ref Functions);
	}

	public void RemoveStyle(string name) {
		int index = System.Array.FindIndex(Functions, x => x.Name == name);
		if(index >= 0) {
			ArrayExtensions.RemoveAt(ref Functions, index);
		} else {
			Debug.Log("Style with name " + name + " does not exist.");
		}
	}

	public float[] GetStyle(Frame frame) {
		float[] style = new float[Functions.Length];
		for(int i=0; i<style.Length; i++) {
			style[i] = Functions[i].GetValue(frame);
		}
		return style;
	}

	public string[] GetNames() {
		string[] names = new string[Functions.Length];
		for(int i=0; i<names.Length; i++) {
			names[i] = Functions[i].Name;
		}
		return names;
	}

	public void ToggleKey(Frame frame) {
		Keys[frame.Index-1] = !Keys[frame.Index-1];
		for(int i=0; i<Functions.Length; i++) {
			Functions[i].Compute(frame);
		}
	}

	public bool IsKey(Frame frame) {
		return Keys[frame.Index-1];
	}

	public Frame GetPreviousKey(Frame frame) {
		while(frame.Index > 1) {
			frame = frame.GetPreviousFrame();
			if(IsKey(frame)) {
				return frame;
			}
		}
		return null;
	}

	public Frame GetNextKey(Frame frame) {
		while(frame.Index < Data.GetTotalFrames()) {
			frame = frame.GetNextFrame();
			if(IsKey(frame)) {
				return frame;
			}
		}
		return null;
	}

	public override void Draw(MotionEditor editor) {

	}

	protected override void DerivedInspector(MotionEditor editor) {
		Repair();

		Frame frame = editor.GetCurrentFrame();

		if(Utility.GUIButton("Key", IsKey(frame) ? UltiDraw.Cyan : UltiDraw.DarkGrey, IsKey(frame) ? UltiDraw.Black : UltiDraw.White)) {
			ToggleKey(frame);
		}
		Color[] colors = UltiDraw.GetRainbowColors(Functions.Length);
		for(int i=0; i<Functions.Length; i++) {
			float height = 25f;
			EditorGUILayout.BeginHorizontal();
			if(Utility.GUIButton(Functions[i].Name, colors[i].Transparent(Utility.Normalise(Functions[i].GetValue(frame), 0f, 1f, 0.25f, 1f)), UltiDraw.White, 200f, height)) {
				Functions[i].Toggle(frame);
			}
			Rect c = EditorGUILayout.GetControlRect();
			Rect r = new Rect(c.x, c.y, Functions[i].GetValue(frame) * c.width, height);
			EditorGUI.DrawRect(r, colors[i].Transparent(0.75f));
			EditorGUILayout.FloatField(Functions[i].GetValue(frame), GUILayout.Width(50f));
			Functions[i].Name = EditorGUILayout.TextField(Functions[i].Name);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.BeginHorizontal();
		if(Utility.GUIButton("Add Style", UltiDraw.DarkGrey, UltiDraw.White)) {
			AddStyle("Style");
			EditorGUIUtility.ExitGUI();
		}
		if(Utility.GUIButton("Remove Style", UltiDraw.DarkGrey, UltiDraw.White)) {
			RemoveStyle();
			EditorGUIUtility.ExitGUI();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		if(Utility.GUIButton("<", UltiDraw.DarkGrey, UltiDraw.White, 25f, 50f)) {
			Frame previous = GetPreviousKey(frame);
			editor.LoadFrame(previous == null ? 0f : previous.Timestamp);
		}
		EditorGUILayout.BeginVertical(GUILayout.Height(50f));
		Rect ctrl = EditorGUILayout.GetControlRect();
		Rect rect = new Rect(ctrl.x, ctrl.y, ctrl.width, 50f);
		EditorGUI.DrawRect(rect, UltiDraw.Black);

		UltiDraw.Begin();

		float startTime = frame.Timestamp-editor.GetWindow()/2f;
		float endTime = frame.Timestamp+editor.GetWindow()/2f;
		if(startTime < 0f) {
			endTime -= startTime;
			startTime = 0f;
		}
		if(endTime > Data.GetTotalTime()) {
			startTime -= endTime-Data.GetTotalTime();
			endTime = Data.GetTotalTime();
		}
		startTime = Mathf.Max(0f, startTime);
		endTime = Mathf.Min(Data.GetTotalTime(), endTime);
		int start = Data.GetFrame(startTime).Index;
		int end = Data.GetFrame(endTime).Index;
		int elements = end-start;

		Vector3 prevPos = Vector3.zero;
		Vector3 newPos = Vector3.zero;
		Vector3 bottom = new Vector3(0f, rect.yMax, 0f);
		Vector3 top = new Vector3(0f, rect.yMax - rect.height, 0f);

		//Sequences
		for(int i=0; i<Data.Sequences.Length; i++) {
			float _start = (float)(Mathf.Clamp(Data.Sequences[i].Start, start, end)-start) / (float)elements;
			float _end = (float)(Mathf.Clamp(Data.Sequences[i].End, start, end)-start) / (float)elements;
			float left = rect.x + _start * rect.width;
			float right = rect.x + _end * rect.width;
			Vector3 a = new Vector3(left, rect.y, 0f);
			Vector3 b = new Vector3(right, rect.y, 0f);
			Vector3 c = new Vector3(left, rect.y+rect.height, 0f);
			Vector3 d = new Vector3(right, rect.y+rect.height, 0f);
			UltiDraw.DrawTriangle(a, c, b, UltiDraw.Yellow.Transparent(0.25f));
			UltiDraw.DrawTriangle(b, c, d, UltiDraw.Yellow.Transparent(0.25f));
		}

		//Styles
		for(int i=0; i<Functions.Length; i++) {
			Frame current = Data.GetFirstFrame();
			while(current != Data.GetLastFrame()) {
				Frame next = GetNextKey(current);
				float _start = (float)(Mathf.Clamp(current.Index, start, end)-start) / (float)elements;
				float _end = (float)(Mathf.Clamp(next.Index, start, end)-start) / (float)elements;
				float xStart = rect.x + _start * rect.width;
				float xEnd = rect.x + _end * rect.width;
				float yStart = rect.y + (1f - Functions[i].Values[Mathf.Clamp(current.Index, start, end)-1]) * rect.height;
				float yEnd = rect.y + (1f - Functions[i].Values[Mathf.Clamp(next.Index, start, end)-1]) * rect.height;
				UltiDraw.DrawLine(new Vector3(xStart, yStart, 0f), new Vector3(xEnd, yEnd, 0f), colors[i]);
				current = next;
			}
		}

		//Keys
		for(int i=0; i<Keys.Length; i++) {
			if(Keys[i]) {
				top.x = rect.xMin + (float)(i+1-start)/elements * rect.width;
				bottom.x = rect.xMin + (float)(i+1-start)/elements * rect.width;
				UltiDraw.DrawLine(top, bottom, UltiDraw.White);
			}
		}

		//Current Pivot
		top.x = rect.xMin + (float)(frame.Index-start)/elements * rect.width;
		bottom.x = rect.xMin + (float)(frame.Index-start)/elements * rect.width;
		UltiDraw.DrawLine(top, bottom, UltiDraw.Yellow);
		UltiDraw.DrawCircle(top, 3f, UltiDraw.Green);
		UltiDraw.DrawCircle(bottom, 3f, UltiDraw.Green);

		UltiDraw.End();
		EditorGUILayout.EndVertical();
		if(Utility.GUIButton(">", UltiDraw.DarkGrey, UltiDraw.White, 25f, 50f)) {
			Frame next = GetNextKey(frame);
			editor.LoadFrame(next == null ? Data.GetTotalTime() : next.Timestamp);
		}
		EditorGUILayout.EndHorizontal();
	}

	[System.Serializable]
	public class StyleFunction {
		public StyleModule Module;
		public string Name;
		public float[] Values;
		public bool[] Flags;

		public StyleFunction(StyleModule module, string name) {
			Module = module;
			Name = name;
			Values = new float[Module.Data.GetTotalFrames()];
			Flags = new bool[Module.Data.GetTotalFrames()];
		}

		public float GetValue(Frame frame) {
			return Values[frame.Index-1];
		}

		public void Toggle(Frame frame) {
			if(Module.IsKey(frame)) {
				Values[frame.Index-1] = GetValue(frame) == 1f ? 0f : 1f;
				Compute(frame);
			}
		}

		public void Compute(Frame frame) {
			Frame current = frame;
			Frame previous = Module.GetPreviousKey(current);
			previous = previous == null ? Module.Data.GetFrame(1) : previous;
			Frame next = Module.GetNextKey(current);
			next = next == null ? Module.Data.GetFrame(Module.Data.GetTotalFrames()) : next;

			if(Module.IsKey(frame)) {
				//Current Frame
				Values[current.Index-1] = GetValue(current);
				//Previous Frames
				if(previous != frame) {
					float valA = GetValue(previous);
					float valB = GetValue(current);
					for(int i=previous.Index; i<current.Index; i++) {
						float weight = (float)(i-previous.Index) / (float)(frame.Index - previous.Index);
						Values[i-1] = (1f-weight) * valA + weight * valB;
					}
				}
				//Next Frames
				if(next != frame) {
					float valA = GetValue(current);
					float valB = GetValue(next);
					for(int i=current.Index+1; i<=next.Index; i++) {
						float weight = (float)(i-current.Index) / (float)(next.Index - current.Index);
						Values[i-1] = (1f-weight) * valA + weight * valB;
					}
				}
			} else {
				float valA = GetValue(previous);
				float valB = GetValue(next);
				for(int i=previous.Index; i<=next.Index; i++) {
					float weight = (float)(i-previous.Index) / (float)(next.Index - previous.Index);
					Values[i-1] = (1f-weight) * valA + weight * valB;
				}
			}
		}
	}

}
#endif