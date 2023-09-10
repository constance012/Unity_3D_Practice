using System.Collections;
using UnityEngine;

namespace CSTGames.Utility
{
	public sealed class CoroutineWithData
	{
		public Coroutine coroutine { get; private set; }
		public object yieldedData = default;

		private IEnumerator _target;

		public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
		{
			this._target = target;
			this.coroutine = owner.StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			while (_target.MoveNext())
			{
				yieldedData = _target.Current;
				yield return yieldedData;
			}
		}
	}
}