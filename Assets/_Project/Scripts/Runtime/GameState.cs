namespace PhysicsStack
{
    /// <summary>
    /// Oyunun tek doğrusu. Gün 4'teki debug paneli doğrudan bunu ekrana basacak;
    /// "şu an ne oluyor" sorusunun cevabı tek yerde dursun diye enum olarak duruyor.
    /// </summary>
    public enum GameState
    {
        /// <summary>Menü açık; ortada bir tur yok.</summary>
        Menu,

        /// <summary>Sıradaki kutu havada asılı, oyuncunun dokunmasını bekliyor.</summary>
        WaitingForDrag,

        /// <summary>Kutu parmakta.</summary>
        Dragging,

        /// <summary>Kutu bırakıldı, yığın hâlâ hareket hâlinde.</summary>
        Settling,

        /// <summary>
        /// Kule hedefi geçti ama henüz kazanılmadı: tutunması bekleniyor.
        ///
        /// Bu durum sonradan eklendi. Öncesinde hedefi geçmek tek başına
        /// kazandırıyordu ve hafifçe kayan bir kule "kazandın" yazdıktan on
        /// saniye sonra devrilebiliyordu. Geçmek bir an, tutunmak bir süre.
        /// </summary>
        Holding,

        /// <summary>Yığın hedefi geçti ve orada tutundu.</summary>
        Won,

        /// <summary>Bir parça zeminin altına düştü.</summary>
        Lost,
    }
}
