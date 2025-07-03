using AutoMapper;
using SecretHitlerBackend.Models;

namespace SecretHitlerBackend.Profiles;

public class Profiles : Profile
{
    public Profiles()
    {
        CreateMap<Member, Player>();
    }
}
