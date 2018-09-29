using AutoMapper;
using CabinetServer.Data.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinetServer.Data
{
    public class DtoMapperProfile : Profile
    {
        public DtoMapperProfile()
        {
            CreateMap<ApplicationUser, ApplicationUserDto>();
            CreateMap<ApplicationUserDto, ApplicationUser>();
        }
    }
}
