using ECommerce.Application.Services;
using System.Collections.Concurrent;

namespace ECommerce.Infrastructure.Services
{
    public class InMemoryOtpStore : IOtpStore
    {
        private readonly ConcurrentDictionary<string, OtpEntry> _entries = new();

        public OtpEntry? Get(string phoneOrEmail)
        {
            _entries.TryGetValue(phoneOrEmail, out var entry);
            return entry;
        }

        public void Save(OtpEntry entry)
        {
            _entries[entry.PhoneOrEmail] = entry;
        }

        public void Remove(string phoneOrEmail)
        {
            _entries.TryRemove(phoneOrEmail, out _);
        }
    }
}
