using Identity.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Domain.Entities;

/// <summary>
/// Cấu hình quyền riêng tư cá nhân (UC-09) và điều khiển bộ lọc xem hồ sơ (UC-07)
/// </summary>
[Table("UserPrivacySettings")]
public class UserPrivacySetting : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Trạng thái hồ sơ công khai. Nếu false, chỉ chính chủ sở hữu hoặc Admin mới xem được chi tiết.
    /// </summary>
    public bool IsProfilePublic { get; set; } = true;

    /// <summary>
    /// Cho phép hiển thị địa chỉ Email trên hồ sơ công khai.
    /// Mặc định: false (bảo mật thông tin).
    /// </summary>
    public bool ShowEmail { get; set; } = false;

    /// <summary>
    /// Cho phép hiển thị Số điện thoại liên lạc.
    /// Mặc định: false (bảo mật thông tin).
    /// </summary>
    public bool ShowPhoneNumber { get; set; } = false;

    /// <summary>
    /// Cho phép hiển thị liên kết và thống kê GitHub.
    /// </summary>
    public bool ShowGitHubStats { get; set; } = true;

    /// <summary>
    /// Cho phép hiển thị điểm uy tín (Reputation Score) và hoạt động đóng góp.
    /// </summary>
    public bool ShowActivityStats { get; set; } = true;
}
