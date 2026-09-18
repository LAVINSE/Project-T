using System.Collections;
using UnityEngine;

namespace AllIn1SpringsToolkit
{
	/// <summary>
	/// Extension methods for AnchoredPositionSpringComponent providing punch and shake effects.
	/// </summary>
	public static class AnchoredPositionSpringComponentExtensions
	{
		private const float ShakeDotThreshold = 0.4f;

		/// <summary>
		/// Adds velocity in a given direction.
		/// </summary>
		public static void PunchPosition(this AnchoredPositionSpringComponent spring, Vector2 direction, float magnitude)
		{
			spring.AddVelocity(direction.normalized * magnitude);
		}

		/// <summary>
		/// Adds upward velocity.
		/// </summary>
		public static void PunchVertical(this AnchoredPositionSpringComponent spring, float magnitude)
		{
			spring.AddVelocity(Vector2.up * magnitude);
		}

		/// <summary>
		/// Adds rightward velocity.
		/// </summary>
		public static void PunchHorizontal(this AnchoredPositionSpringComponent spring, float magnitude)
		{
			spring.AddVelocity(Vector2.right * magnitude);
		}

		/// <summary>
		/// Adds random velocity in a random direction.
		/// </summary>
		public static void RandomPunch(this AnchoredPositionSpringComponent spring, float magnitude)
		{
			Vector2 randomDir = Random.insideUnitCircle.normalized;
			spring.AddVelocity(randomDir * magnitude);
		}

		/// <summary>
		/// Shakes position for a duration. Use with StartCoroutine.
		/// </summary>
		public static IEnumerator Shake(this AnchoredPositionSpringComponent spring,
			float magnitude, float duration, float frequency = 40f)
		{
			float interval = 1f / frequency;
			int punchCount = Mathf.CeilToInt(duration * frequency);
			Vector2 lastDir = Vector2.zero;

			for (int i = 0; i < punchCount; i++)
			{
				spring.SetVelocity(Vector2.zero);

				Vector2 newDir = Random.insideUnitCircle.normalized;
				if (Vector2.Dot(newDir, lastDir) > ShakeDotThreshold) newDir = -newDir;
				spring.AddVelocity(newDir * magnitude);
				lastDir = newDir;

				yield return new WaitForSeconds(interval);
			}
			spring.SetVelocity(Vector2.zero);
		}
	}
}
