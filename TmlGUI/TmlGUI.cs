using System;
using DxLibDLL;
using System.Collections.Generic;


namespace ThlGUI
{
	public delegate void EventHandler(Control sender, EventArgs args);
	
	public struct pointint
	{
		public int x;
		public int y;

		public static pointint Get(int x, int y)
		{
			pointint rv;
			rv.x = x;
			rv.y = y;
			return rv;
		}

		public static bool Equals(pointint A, pointint B)
		{
			if ((A.x == B.x) && (A.y == B.y))
				return true;
			else
				return false;
		}
	}

	public struct GUIMessage
	{
		public int Message;
		public byte[] KeyBuffer;
		public byte[] KeyBufferMask;
		public pointint MousePoint;
		public int MouseClickState;
		public int MouseWheelInput;

		public void AddMessage(int message)
		{
			this.Message = this.Message | message;
			return;
		}

		public void RemoveMessage(int message)
		{
			this.Message = this.Message & (~message);
			return;
		}

		public void CopyTo(out GUIMessage direction)
		{
			direction.Message = Message;
			direction.MousePoint = MousePoint;
			direction.MouseClickState = MouseClickState;
			direction.MouseWheelInput = MouseWheelInput;
			direction.KeyBuffer = KeyBuffer.Clone () as byte[];
			direction.KeyBufferMask = KeyBufferMask.Clone () as byte[];
			return;
		}
	}

	public class EventArgs
	{
	}

	public class ResourceManager
	{
		public const int MaxResourceForControls = 4;
		public const int ResourceForControl = 0;
		public const int ResourceForForm = 1;
		public const int ResourceForButton = 3;
		public const int ResourceForTextBox = 4;

		public static int[][] ResourceHandle = new int[MaxResourceForControls][];

		/// <summary>
		/// Opens the resource file.
		/// </summary>
		/// <returns>Success = 0, Failed = -1.</returns>
		/// <param name="path">Path to the resource file(.rsc).</param>
		public static int OpenResource(string path)
		{
			return 0;
		}
	}
	
	public class GraphicalUI
	{
		public const int Message_Cursor_Move = 1;
		public const int Message_Mouse_Click = 2;
		public const int Message_MouseWheel_Role = 4;
		public const int Message_Keyboard_Down = 8;
		public const int Message_Keyboard_Up = 16;
		public const int Message_Mouse_LeftClick = 32;
		public const int Message_Mouse_RightClick = 64;
		public const int Message_Mouse_MiddleClick = 128;
		
		private static Func<pointint> MousePointGettingMethod;
		private static Func<int> MouseClickGettingMethod;
		private static Func<byte[]> KeyboardGettingMethod;
		private static Func<int,bool> KeyboardStateGettingMethod;

		private static List<Form> Collections = new List<Form>();
		private static Form FocusedForm;
		
		private static GUIMessage Message;

		public GraphicalUI()
		{
		}

		public static void GraphicalUIinit()
		{
			Message.KeyBuffer = new byte[256];
			Message.KeyBufferMask = new byte[256];
		}

		//CollectionsにFormを追加する。zソートは今はしない
		public static int Add(Form form)
		{
			Collections.Add(form);
			return 0;
		}

		//アクティブなFormを最前面に持っていく
		public static int SortZBuffer()
		{
			Form Buffer;
			for (int i = 0; i < Collections.Count; i++)
			{
				if (Collections[i] == FocusedForm) {
					Buffer = Collections[i];
					Collections.RemoveAt(i);
					Collections.Add(Buffer);
					break;
				}
			}
			return 0;
		}

		//formをFocusedにする(前代のFocusを解除する)
		public static void SetFocus(Form form)
		{
			FocusedForm.Unfocus(null, null);
			FocusedForm = form;
			return;
		}

		public static int SetMousePointGettingMethod(Func<pointint> method)
		{
			MousePointGettingMethod += method;
			return 0;
		}

		public static int SetMouseClickGettingMethod(Func<int> method)
		{
			MouseClickGettingMethod += method;
			return 0;
		}

		public static int SetKeyboardGettingMethod(Func<byte[]> method)
		{
			KeyboardGettingMethod += method;
			return 0;
		}
		public static int SetKeyboardStateGettingMethod(Func<int,bool> method)
		{
			KeyboardStateGettingMethod += method;
			return 0;
		}

		public static int Routine()
		{
			//1.Making Message
			GUIMessage Msg = new GUIMessage();
			//Collect inputs
			Msg.MousePoint = MousePointGettingMethod();
			Msg.MouseClickState = MouseClickGettingMethod();
			Msg.KeyBuffer = KeyboardGettingMethod();
			Msg.KeyBufferMask = Message.KeyBuffer.Clone () as byte[];
			//CheckDifferences
			//Mouse
			//Cursor
			if (!pointint.Equals(Msg.MousePoint, Message.MousePoint)) {
				Msg.AddMessage(Message_Cursor_Move);
			}
			//Button
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_LEFT) != 0)
			{
				Msg.AddMessage(Message_Mouse_LeftClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_RIGHT) != 0)
			{
				Msg.AddMessage(Message_Mouse_RightClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_MIDDLE) != 0)
			{
				Msg.AddMessage(Message_Mouse_MiddleClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			//Wheel
			if (Msg.MouseWheelInput != 0) {
				Msg.AddMessage(Message_MouseWheel_Role);
			}

			//Keyboard
			//省略

			//2.Sending Message
			foreach (Control alge in Collections)
			{
				alge.SendMessage(Msg);
			}

			//Zソート
			if ((Message.Message & Message_Mouse_Click) != 0) {
				SortZBuffer();
			}

			//3.Copy message to buffer
			Message.Message = Msg.Message;
			Message.MouseClickState = Msg.MouseClickState;
			Message.MousePoint = Msg.MousePoint;
			Msg.KeyBuffer.CopyTo(Message.KeyBuffer, 0);
			Message.MouseWheelInput = Msg.MouseWheelInput;

			//4.描画命令(Collectionsはzソートされているのでそのまま描画できる)
			foreach (Control alge in Collections)
			{
				alge.Render();
			}
			return 0;
		}
	}

	public class Control
	{
		protected pointint Position;
		protected pointint Size;
		protected List<Control> Collections = new List<Control>();
		protected Control ParentControl;
		protected string Label;
		protected GUIMessage Message = new GUIMessage();

		//Skins
		protected int SkinTop;
		protected int SkinBottom;
		protected int SkinRight;
		protected int SkinLeft;
		protected int SkinTopLeft;
		protected int SkinTopRight;
		protected int SkinBottomLeft;
		protected int SkinBottomRight;
		protected int SkinBase;

		//Events
		public event EventHandler Click;
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;
		public event EventHandler MouseHover;
		public event EventHandler MouseLeave;
		public event EventHandler MouseMove;
		public event EventHandler MouseWheel;
		public event EventHandler Move;
		public event EventHandler Paint;
			//And more...

		//Properties
		public virtual bool Focused { get; private set; } = false;

		//カーソルがControlの上に乗っているときにtrue
		public virtual bool Pointed { get; private set; } = false;

		public virtual string Text { get; set; }


		public Control()
		{
			//Focusをイベントに関連付ける
			GotFocus += Focus;
			LostFocus += Unfocus;
		}

		/// <summary>
		/// Null例外を避けるイベントInvokeメソッドです.イベントはこれを経由してInvokeすることを推奨します.
		/// </summary>
		/// <param name="e">イベント</param>
		/// <param name="sender">呼び出し主</param>
		/// <param name="args">引数</param>
		public void InvokeEvent(EventHandler e, Control sender, EventArgs args)
		{
			e ?.Invoke (sender, args);
			return;
		}

		public virtual void AddChild(Control control)
		{
			Collections.Add (control);
			return;
		}

		public virtual void RemoveChild(Control control)
		{
			for (int i = 0; i < Collections.Count; i++) {
				if (Collections[i] == control) {
					Collections.RemoveAt (i);
					break;
				}
			}
			return;
		}

		public virtual void SendMessage(GUIMessage message)
		{
			message.CopyTo (out Message);
			//Collectionsにもメッセージを送ります
			foreach (Control alge in Collections)
			{
				alge.SendMessage(message);
			}
			ProcessMessasge ();
		}

		public virtual int Render()
		{
			return 0;
		}

		public virtual int ProcessMessasge()
		{
			//メッセージ処理をここに記述します。イベントの発生もここで行います。
			//Message_Mouse_Click
			if ((Message.Message & GraphicalUI.Message_Mouse_Click) != 0)
			{
				if (Pointed) {
					if (!Focused) {
						InvokeEvent (GotFocus, this, null);
					}
					InvokeEvent (Click, this, null);
				} else if (Focused) {
					InvokeEvent (LostFocus, this, null);
				}
			}
			//Message_Cursor_Move
			if  ((Message.Message & GraphicalUI.Message_Cursor_Move) != 0)
			{
				if ((Message.MousePoint.x >= Position.x) && (Message.MousePoint.x <= Position.x + Size.x) &&
					(Message.MousePoint.y >= Position.y) && (Message.MousePoint.y <= Position.x + Size.y)) {
					if (!Pointed) {
						Pointed = true;
					}
				}
				else {
					if (Pointed) {
						Pointed = false;
						InvokeEvent(MouseLeave, this, null);
					}
				}
			}
			//Mouse Hover
			if (Pointed) {
				InvokeEvent(MouseHover, this, null);
			}

			//Collectionsのメッセージ処理も行います
			foreach (Control alge in Collections)
			{
				alge.ProcessMessasge();
			}
			return 0;
		}


		public virtual void Focus(Control sender, EventArgs args)
		{
			Focused = true;
			return;
		}

		public virtual void Unfocus(Control sender, EventArgs args)
		{
			Focused = false;
			return;
		}
	}

	public class Form : Control
	{
		//Skins
		protected int SkinClose;
		protected int SkinMaximize;
		protected int SkinMinimize;
		protected int SkinStore;
		protected int SkinTitleBarTop;
		protected int SkinTitleBarBottom;
		protected int SkinTitleBarRight;
		protected int SkinTitleBarLeft;
		protected int SkinTitleBarBottomLeft;
		protected int SkinTitleBarBottomRight;

		public Form()
		{
			//Default setting of Form.
			Position.x = 0;
			Position.y = 0;
			Size.x = 300;
			Size.y = 300;
		}

		public Form(int px, int py, int width, int height, string label)
		{
			Position.x = px;
			Position.y = py;
			Size.x = width;
			Size.y = height;
		}

		public override int Render()
		{
			return 0;
		}
	}

	public class Button : Control
	{
		
	}

	public class Textbox : Control
	{
		private int InputHandle;
		private int BufferSize;
		private int SingleCharOnlyFlag;
		private int NumberCharOnlyFlag;
		private System.Text.StringBuilder Buffer;
		private string Text { get; set; }

		public Textbox()
		{
			GotFocus += InputAwait;
			LostFocus += InputUnawait;
		}

		private void InputAwait(Control sender, EventArgs args)
		{
			if (InputHandle == -1) {
				InputHandle = DX.MakeKeyInput (BufferSize, DX.FALSE, SingleCharOnlyFlag, NumberCharOnlyFlag);
			}
			DX.SetActiveKeyInput (InputHandle);
			return;
		}

		private void InputUnawait(Control sender, EventArgs args)
		{
			Text = GetBuffer ();
			DX.DeleteKeyInput (InputHandle);
			InputHandle = -1;
			return;
		}

		private string GetBuffer()
		{
			if (InputHandle == -1) {
				return Text;
			} else {
				if (Buffer == null) {
					Buffer = new System.Text.StringBuilder (BufferSize);
				}
				Buffer = new System.Text.StringBuilder (BufferSize);
				DX.GetKeyInputString (Buffer, InputHandle);
			}
			return Buffer.ToString ();
		}

	}

	public class Label : Control
	{
	}

	public class ScrollBar : Control
	{
	}

	public class HScrollBar : ScrollBar
	{
	}

	public class VScrollBar : ScrollBar
	{
	}

	public class ListControl : Control
	{
	}
}
