namespace PhysicsStack
{
    /// <summary>
    /// Oyunun tek doğrusu. Gün 4'teki debug paneli doğrudan bunu ekrana basacak;
    /// "şu an ne oluyor" sorusunun cevabı tek yerde dursun diye enum olarak duruyor.
    /// </summary>
    public enum GameState
    {
        /// <summary>Sıradaki kutu havada asılı, oyuncunun dokunmasını bekliyor.</summary>
        WaitingForDrag,

        /// <summary>Kutu parmakta.</summary>
        Dragging,

        /// <summary>Kutu bırakıldı, yığın hâlâ hareket hâlinde.</summary>
        Settling,

        /// <summary>Yığın oturdu ve hedef yüksekliği geçti.</summary>
        Won,

        /// <summary>Bir parça zeminin altına düştü.</summary>
        Lost,
    }
}
