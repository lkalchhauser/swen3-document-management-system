using AutoMapper;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.Model.ORM;

namespace DocumentManagementSystem.Application.Mapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			// ORM → DTO
			CreateMap<Document, DocumentDTO>()
				 .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name)));

			CreateMap<DocumentMetadata, DocumentMetadataDTO>();
			CreateMap<Tag, TagDTO>();

			// DTO → ORM
			CreateMap<DocumentCreateDTO, Document>()
				 .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(name => new Tag { Name = name, CreatedAt = DateTimeOffset.UtcNow})));

			CreateMap<DocumentMetadataDTO, DocumentMetadata>();
			CreateMap<TagDTO, Tag>();
		}
	}
}
