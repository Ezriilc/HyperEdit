//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//
// Thanks to Payo for inventing, writing and contributing the PlanetEditor component.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyperEdit
{
  public abstract class FieldProxy
  {
    public string Name { get; protected set; }

    protected TextBox _textBox;
    public TextBox NewTextBox()
    {
      _textBox = new TextBox(Name, StringValue);
      return _textBox;
    }
    public abstract string StringValue { get; }
    public string CurrentInput { get { return _textBox == null ? null : _textBox.Value; } }

    public static FieldProxy Create<T>(string name, Func<T> get, Action<T> set)
    {
      var result = new FieldProxy<T>(get, set);
      result.Name = name;

      return result;
    }

    public static FieldProxy Create<T>(string name, Func<CelestialBody> getBody, string fieldName)
    {
      Func<T> get = () =>
      {
        CelestialBody body = getBody();

        if (body == null)
          return default(T);

        var fieldAccess = body.GetType().GetField(fieldName);

        if (fieldAccess == null)
          return default(T);

        object result = fieldAccess.GetValue(body);

        if (result == null)
          return default(T);

        return (T)result;        
      };

      Action<T> set = v =>
      {
        CelestialBody body = getBody();
        var fieldAccess = body.GetType().GetField(fieldName);

        fieldAccess.SetValue(body, v);
      };

      var proxy = Create<T>(name, get, set);
      return proxy;
    }

    public abstract bool CanParseTextBox();
    public abstract void Commit();
  }

  public class FieldProxy<T> : FieldProxy
  {
    public Func<T> Get { get; protected set; }
    public Action<T> Set { get; protected set; }

    public FieldProxy(Func<T> get, Action<T> set)
    {
      Get = get;
      Set = set;
    }

    public override string StringValue
    {
      get { return Get().ToString(); }
    }

    public override bool CanParseTextBox()
    {
      string value = _textBox.Value;
      if (string.IsNullOrEmpty(value))
      {
        ErrorPopup.Error(string.Format("Planet parameter [{0}] was empty", Name));
        return false;
      }

      T unused;
      if (!TryParse(out unused))
      {
        ErrorPopup.Error(string.Format("Planet parameter [{0}] failed to parse from [{1}]", Name, StringValue));
        return false;
      }

      return true;
    }

    protected bool TryParse(out T v)
    {
      // generic cannot parse, find specialized

      v = default(T);
      T temp = v;
      bool ret = false;

      if (typeof(T) == typeof(string))
        ret = TryParseSpeciliazed((string r) => { temp = (T)((object)r); });
      if (typeof(T) == typeof(bool))
        ret = TryParseSpeciliazed((bool r) => { temp = (T)((object)r); });
      if (typeof(T) == typeof(double))
        ret = TryParseSpeciliazed((double r) => { temp = (T)((object)r); });
      if (typeof(T) == typeof(float))
        ret = TryParseSpeciliazed((float r) => { temp = (T)((object)r); });
      if (typeof(T) == typeof(UnityEngine.Color))
        ret = TryParseSpeciliazed((UnityEngine.Color r) => { temp = (T)((object)r); });

      if (!ret)
      {
        ErrorPopup.Error("generic try parse used for type: " + v.GetType().ToString());
      }

      v = temp;
      return ret;
    }

    protected bool TryParseSpeciliazed(Action<string> set)
    {
      string result = CurrentInput;
      set(result);
      return !string.IsNullOrEmpty(result);
    }

    protected bool TryParseSpeciliazed(Action<bool> set)
    {
      bool result;
      
      bool ret = bool.TryParse(CurrentInput, out result);
      set(result);
      return ret;
    }

    protected bool TryParseSpeciliazed(Action<double> set)
    {
      double result;
      bool ret = double.TryParse(CurrentInput, out result);
      set(result);
      return ret;
    }

    protected bool TryParseSpeciliazed(Action<float> set)
    {
      float result;
      bool ret = float.TryParse(CurrentInput, out result);
      set(result);
      return ret;
    }

    protected bool TryParseSpeciliazed(Action<UnityEngine.Color> set)
    {
      string v = CurrentInput;
      /// RGBA(0.224, 0.194, 0.306, 1.000)

      if (!TestAndMove(ref v, "RGBA"))
        return false;

      if (!TestAndMove(ref v, "("))
        return false;

      float r, g, b, a;

      if (!ReadFloat(ref v, out r))
        return false;

      if (!TestAndMove(ref v, ","))
        return false;

      if (!ReadFloat(ref v, out g))
        return false;

      if (!TestAndMove(ref v, ","))
        return false;

      if (!ReadFloat(ref v, out b))
        return false;

      if (!TestAndMove(ref v, ","))
        return false;

      if (!ReadFloat(ref v, out a))
        return false;

      if (!TestAndMove(ref v, ")"))
        return false;

      if (!string.IsNullOrEmpty(v))
        return false;

      set(new UnityEngine.Color(r, g, b, a));

      return true;
    }

    private bool ReadFloat(ref string input, out float result)
    {
      result = default(float);

      if (string.IsNullOrEmpty(input))
        return false;

      int length = 0;

      while (true)
      {
        if (length >= input.Length)
          break;

        if (!char.IsDigit(input[length]) && input[length] != '.')
          break;

        length++;
      }

      if (length == 0)
        return false;

      string numberText = input.Substring(0, length);
      input = input.Substring(length);
      input = input.Trim();

      return float.TryParse(numberText, out result);
    }

    private bool TestAndMove(ref string input, string test)
    {
      if (input == null)
        return false;

      if (!input.StartsWith(test))
        return false;

      if (input.Length == test.Length)
        input = string.Empty;
      else
        input = input.Substring(test.Length);

      input = input.Trim();

      return true;
    }

    public override void Commit()
    {
      T result;
      if (!TryParse(out result))
      {
        ErrorPopup.Error("Failed to parse for commit. " + Name + " = " + CurrentInput);
        return;
      }

      Set(result);
    }
  }
}