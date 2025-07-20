using System;
using AdhdProductivitySystem.Domain.Common;

namespace AdhdProductivitySystem.Domain.Entities
{
    /// <summary>
    /// Refresh Token 實體，用於管理使用者的 refresh token
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        /// <summary>
        /// 關聯的使用者 ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Token 值
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token 過期時間
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 裝置識別碼（可選）
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 是否已撤銷
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 撤銷時間
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 關聯的使用者
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// 檢查 Token 是否有效
        /// </summary>
        public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// 撤銷 Token
        /// </summary>
        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
    }
}