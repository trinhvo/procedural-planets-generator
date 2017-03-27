﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyEngine
{

	public class CVar
	{
		//public dynamic Value { get; set; }
		public string name;

		bool _bool;

		MyDebug debug;

		public bool Bool
		{
			get
			{
				return _bool;
			}
			set
			{
				if (_bool != value)
				{
					_bool = value;
					debug.Info(name + " changed to: " + value);
					OnChanged?.Invoke(this);
				}
			}
		}


		public OpenTK.Input.Key toogleKey = OpenTK.Input.Key.Unknown;

		public event Action<CVar> OnChanged;

		public CVar(MyDebug debug)
		{
			this.debug = debug;
		}

		public CVar OnChangedAndNow(Action<CVar> action)
		{
			if (action != null)
			{
				OnChanged += action;
				action.Invoke(this);
			}
			return this;
		}

		/*
		public CVar InitializeWith(bool value)
		{
			_bool = value;
			debug.Info(name + " changed to: " + value);
			OnChanged?.Invoke(this);
			return this;
		}
		*/

		public bool EatBoolIfTrue()
		{
			if (Bool)
			{
				Bool = false;
				return true;
			}
			return false;
		}

		public CVar Toogle()
		{
			Bool = !Bool;
			return this;
		}

		public CVar ToogledByKey(OpenTK.Input.Key key)
		{
			debug.Info($"{key} to toggle {name}");
			toogleKey = key;
			return this;
		}

		public static implicit operator bool(CVar cvar) => cvar.Bool;
	}

}
