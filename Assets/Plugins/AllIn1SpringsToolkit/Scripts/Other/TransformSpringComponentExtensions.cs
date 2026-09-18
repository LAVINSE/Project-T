using System.Collections;
using UnityEngine;

namespace AllIn1SpringsToolkit
{
	/// <summary>
	/// Extension methods for TransformSpringComponent providing punch and shake effects.
	/// </summary>
	public static class TransformSpringComponentExtensions
	{
		private const float ShakeDotThreshold = 0.4f;

		#region SCALE

		/// <summary>
		/// Adds uniform scale velocity without changing force/drag.
		/// </summary>
		public static void PunchScale(this TransformSpringComponent spring,
			float baseScale, float multiplier = 1f)
		{
			spring.AddVelocityScale(Vector3.one * (baseScale * multiplier));
		}

		/// <summary>
		/// Adds random scale velocity on X/Y axes.
		/// </summary>
		public static void RandomPunchScale(this TransformSpringComponent spring, float magnitude)
		{
			Vector2 randomDir = Random.insideUnitCircle.normalized;
			spring.AddVelocityScale(new Vector3(randomDir.x, randomDir.y, 0f) * magnitude);
		}

		/// <summary>
		/// Shakes scale on X/Y axes for a duration. Use with StartCoroutine.
		/// </summary>
		public static IEnumerator ShakeScale(this TransformSpringComponent spring,
			float magnitude, float duration, float frequency = 40f)
		{
			float interval = 1f / frequency;
			int punchCount = Mathf.CeilToInt(duration * frequency);
			Vector2 lastDir = Vector2.zero;

			for (int i = 0; i < punchCount; i++)
			{
				spring.SetVelocityScale(Vector3.zero);

				Vector2 newDir = Random.insideUnitCircle.normalized;
				if (Vector2.Dot(newDir, lastDir) > ShakeDotThreshold) newDir = -newDir;
				spring.AddVelocityScale(new Vector3(newDir.x, newDir.y, 0f) * magnitude);
				lastDir = newDir;

				yield return new WaitForSeconds(interval);
			}
			spring.SetVelocityScale(Vector3.zero);
		}

		#endregion

		#region ROTATION

		/// <summary>
		/// Adds Z-axis rotation velocity. Resets existing rotation velocity first.
		/// </summary>
		public static void PunchRotation(this TransformSpringComponent spring,
			float baseRotation, float multiplier = 1f, bool randomDirection = true)
		{
			float direction = randomDirection && Random.value > 0.5f ? 1f : -1f;
			spring.SetVelocityRotation(Vector3.zero);
			spring.AddVelocityRotation(new Vector3(0f, 0f, direction * multiplier * baseRotation));
		}

		/// <summary>
		/// Adds random Z-axis rotation velocity with random sign.
		/// </summary>
		public static void RandomPunchRotation(this TransformSpringComponent spring, float magnitude)
		{
			float randomSign = Random.value > 0.5f ? 1f : -1f;
			spring.AddVelocityRotation(Vector3.forward * randomSign * magnitude);
		}

		/// <summary>
		/// Shakes rotation on Z-axis for a duration. Use with StartCoroutine.
		/// </summary>
		public static IEnumerator ShakeRotation(this TransformSpringComponent spring,
			float magnitude, float duration, float frequency = 40f)
		{
			float interval = 1f / frequency;
			int punchCount = Mathf.CeilToInt(duration * frequency);
			float sign = Random.value > 0.5f ? 1f : -1f;

			for (int i = 0; i < punchCount; i++)
			{
				spring.SetVelocityRotation(Vector3.zero);
				spring.AddVelocityRotation(Vector3.forward * sign * magnitude);
				sign = -sign;

				yield return new WaitForSeconds(interval);
			}
			spring.SetVelocityRotation(Vector3.zero);
		}

		#endregion

		#region POSITION

		/// <summary>
		/// Adds position velocity in a given direction.
		/// </summary>
		public static void PunchPosition(this TransformSpringComponent spring,
			Vector3 direction, float magnitude)
		{
			spring.AddVelocityPosition(direction.normalized * magnitude);
		}

		/// <summary>
		/// Adds upward position velocity.
		/// </summary>
		public static void PunchPositionVertical(this TransformSpringComponent spring, float magnitude)
		{
			spring.AddVelocityPosition(Vector3.up * magnitude);
		}

		/// <summary>
		/// Adds rightward position velocity.
		/// </summary>
		public static void PunchPositionHorizontal(this TransformSpringComponent spring, float magnitude)
		{
			spring.AddVelocityPosition(Vector3.right * magnitude);
		}

		/// <summary>
		/// Adds random position velocity on X/Y axes. Returns the direction used.
		/// </summary>
		public static Vector2 RandomPunchPosition(this TransformSpringComponent spring, float magnitude)
		{
			Vector2 randomDir = Random.insideUnitCircle.normalized;
			spring.AddVelocityPosition((Vector3)randomDir * magnitude);
			return randomDir;
		}

		/// <summary>
		/// Shakes position on X/Y axes for a duration. Use with StartCoroutine.
		/// </summary>
		public static IEnumerator ShakePosition(this TransformSpringComponent spring,
			float magnitude, float duration, float frequency = 40f)
		{
			float interval = 1f / frequency;
			int punchCount = Mathf.CeilToInt(duration * frequency);
			Vector2 lastDir = Vector2.zero;

			for (int i = 0; i < punchCount; i++)
			{
				spring.SetVelocityPosition(Vector3.zero);

				Vector2 newDir = Random.insideUnitCircle.normalized;
				if (Vector2.Dot(newDir, lastDir) > ShakeDotThreshold) newDir = -newDir;
				spring.AddVelocityPosition((Vector3)newDir * magnitude);
				lastDir = newDir;

				yield return new WaitForSeconds(interval);
			}
			spring.SetVelocityPosition(Vector3.zero);
		}

		#endregion
	}
}
