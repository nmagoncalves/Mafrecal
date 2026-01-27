// Decompiled with JetBrains decompiler
// Type: MafrecalApiV10.StringExtensions
// Assembly: MafrecalApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B5E0A4AC-2A4C-4D6E-A0C6-95F4A5D08D9A
// Assembly location: D:\DevPessoal\MafrecalApi.dll

using System;

namespace MafrecalApiV10
{
  public static class StringExtensions
  {
    public static string Left(this string value, int maxLength)
    {
      if (string.IsNullOrEmpty(value))
        return value;
      maxLength = Math.Abs(maxLength);
      return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
  }
}
