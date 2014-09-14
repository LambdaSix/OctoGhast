using System;

namespace RenderLike
{
    public class Rand
    {
         Random _rnd { get; set; }

        public Rand() {
            _rnd = new Random();
        }

        public Rand(int seed) {
            _rnd = new Random(seed);
        }

        public Rand(Random random) {
            _rnd = random;
        }

        /// <summary>
        /// Returns 0..1
        /// </summary>
        /// <returns></returns>
        public int GetInt() {
            return _rnd.Next(0, 2);
        }

        /// <summary>
        /// Return a number between <paramref name="min"/> and <paramref name="max"/>, inclusive.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public int GetInt(int min, int max) {
            return _rnd.Next(min, max + 1);
        }

        public bool GetBoolean() {
            return GetInt() == 0;
        }

        public float GetFloat() {
            return (float)_rnd.NextDouble();
        }

        public T PickFrom<T>(params T[] choices) {
            return choices[GetInt(0, choices.Length)];
        }

        public T FromEnum<T>() {
            return PickFrom((T[])Enum.GetValues(typeof (T)));
        }
    }
}