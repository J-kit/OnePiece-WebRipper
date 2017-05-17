using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RipLib
{
	public static class Extensions
	{
		public static void AddRow<T>(this DataGridView dgv, string[] input) where T : DataGridViewCell, new()
		{
			dgv.Rows.Add(GetRow<T>(input));
		}

		public static void AddRows<T>(this DataGridView dgv, IEnumerable<string[]> input) where T : DataGridViewCell, new()
		{
			var resRows = input.Select(m => GetRow<T>(m)).ToArray();
			dgv.Rows.AddRange(resRows);
		}

		public static DataGridViewRow GetRow<T>(string[] input) where T : DataGridViewCell, new()
		{
			var row = new DataGridViewRow();
			var cells = input.Select(m => new T { Value = m }).Cast<DataGridViewCell>().ToArray();
			row.Cells.AddRange(cells);
			return row;
		}

		public static void DownloadStringAsync(this WebClient srcWeb, string uri) => srcWeb.DownloadStringAsync(new Uri(uri));

		public static IEnumerable<Match> MatchesMinGroups(this Regex input, string source, int minCount)
			=> input.Matches(source).MinGroups(minCount);

		/// <summary>
		/// Returns all Matches which Groups atleast contains <see cref="minCount"/> Values
		/// </summary>
		/// <param name="input"></param>
		/// <param name="minCount"></param>
		/// <returns></returns>
		public static IEnumerable<Match> MinGroups(this MatchCollection input, int minCount)
			=> input.Cast<Match>().Where(m => m.Groups.Count >= minCount);
	}

	public static class InvokeExtentions
	{
		public static TResult InvokeEx<TControl, TResult>(this TControl control, Func<TControl, TResult> func) where TControl : Control
			=> control.InvokeRequired ? (TResult)control.Invoke(func, control) : func(control);

		public static void InvokeEx<TControl>(this TControl control, Action<TControl> func) where TControl : Control
			=> control.InvokeEx(c => { func(c); return c; });

		public static void InvokeEx<TControl>(this TControl control, Action action) where TControl : Control
			=> control.InvokeEx(c => action());
	}
}