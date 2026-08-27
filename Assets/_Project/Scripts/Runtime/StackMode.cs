namespace PhysicsStack
{
    /// <summary>
    /// Sahnenin hangi kural setiyle açılacağı. Şimdilik Inspector'dan seçiliyor;
    /// Gün 9'da mod seçim ekranı bu değeri belirleyecek.
    /// </summary>
    public enum StackMode
    {
        /// <summary>Hedef yüksekliği olan, kutu sınırı olabilen tur.</summary>
        Level,

        /// <summary>Hedefi olmayan tur: bir parça düşene kadar yığıyorsun.</summary>
        Endless,
    }
}
