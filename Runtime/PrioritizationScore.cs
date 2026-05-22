using System;

namespace RinaUtilityAI {
	[Serializable]
	public struct PrioritizationScore {

		public int score;

		public bool absoluteFalse;

		public bool absoluteTrue;

		public static PrioritizationScore operator +(PrioritizationScore a, PrioritizationScore b) {

			if (a.absoluteFalse && b.absoluteTrue || a.absoluteTrue && b.absoluteFalse) {
				return new PrioritizationScore() {
					absoluteTrue = false,
					absoluteFalse = true,
				};
			}

			return new PrioritizationScore {
				score = a.score + b.score,
				absoluteFalse = a.absoluteFalse || b.absoluteFalse,
				absoluteTrue = a.absoluteTrue || b.absoluteTrue
			};
		}

		public static bool operator >(PrioritizationScore a, PrioritizationScore b) {
			if (a.absoluteTrue && !b.absoluteTrue) {
				return true;
			}
			if (!a.absoluteTrue && b.absoluteTrue) {
				return false;
			}
			if (a.absoluteFalse && !b.absoluteFalse) {
				return false;
			}
			if (!a.absoluteFalse && b.absoluteFalse) {
				return true;
			}
			return a.score > b.score;
		}

		public static bool operator < (PrioritizationScore a, PrioritizationScore b) {
			if (a.absoluteTrue != b.absoluteTrue) {
				return !a.absoluteTrue && b.absoluteTrue;
			}
			if (a.absoluteFalse != b.absoluteFalse) {
				return a.absoluteFalse && !b.absoluteFalse;
			}
			return a.score < b.score;
		}

	}
}
