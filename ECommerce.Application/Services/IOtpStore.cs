namespace ECommerce.Application.Services
{
    public interface IOtpStore
    {
        OtpEntry? Get(string phoneOrEmail);
        void Save(OtpEntry entry);
        void Remove(string phoneOrEmail);
    }
}
