using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_rpg.Data;
using dotnet_rpg.Dtos.Character;
using dotnet_rpg.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace dotnet_rpg.Services.CharacterService
{
    public class CharacterService : ICharacterService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CharacterService(IMapper mapper, DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;

        }

        private int GetUserId() => int.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        public async Task<ServiceResponse<List<GetCharacterDto>>> AddCharacter(AddCharacterDto newCharacter)
        {
            var serviceReposnse = new ServiceResponse<List<GetCharacterDto>>();
            Character character = _mapper.Map<Character>(newCharacter);
            character.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == GetUserId());

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();
            serviceReposnse.Data = await _context.Characters
                .Where(c => c.User.Id == GetUserId())
                .Select(c => _mapper.Map<GetCharacterDto>(c)).ToListAsync();
            return serviceReposnse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> DeleteCharacter(int id)
        {
            var serviceReposnse = new ServiceResponse<List<GetCharacterDto>>();
            try
            {
                Character character = await _context.Characters.FirstOrDefaultAsync(c => c.Id == id && c.User.Id == GetUserId());
                if(character != null)
                {
                    _context.Characters.Remove(character);
                    await _context.SaveChangesAsync();
                    serviceReposnse.Data = _context.Characters
                        .Where(c => c.User.Id == GetUserId())
                        .Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();

                }
                else
                {
                    serviceReposnse.Success = false;
                    serviceReposnse.Message = "Character not found";
                }
            }
            catch (Exception ex)
            {
                serviceReposnse.Success = false;
                serviceReposnse.Message = ex.Message;
            }
            return serviceReposnse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> GetAllCharacters()
        {
            var serviceReposnse = new ServiceResponse<List<GetCharacterDto>>();
            var dbCharacters = await _context.Characters
                .Include(c => c.Weapon)
                .Include(c => c.Skills)                
                .Where(c => c.User.Id == GetUserId()).ToListAsync();
            serviceReposnse.Data = dbCharacters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();
            return serviceReposnse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> GetCharacterById(int id)
        {
            var serviceReposnse = new ServiceResponse<GetCharacterDto>();
            var dbCharacter = await _context.Characters
                .Include(c => c.Weapon)
                .Include(c => c.Skills)
                .FirstOrDefaultAsync(c => c.Id == id && c.User.Id == GetUserId());
            serviceReposnse.Data = _mapper.Map<GetCharacterDto>(dbCharacter);
            return serviceReposnse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> UpdateCharacter(UpdateCharacterDto updatedCharacter)
        {
            var serviceReposnse = new ServiceResponse<GetCharacterDto>();
            try
            {
                Character character = await _context.Characters
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == updatedCharacter.Id);

                if(character.User.Id == GetUserId())
                {
                    character.Name = updatedCharacter.Name;
                    character.HitPoints = updatedCharacter.HitPoints;
                    character.Strength = updatedCharacter.Strength;
                    character.Defense = updatedCharacter.Defense;
                    character.Intelligence = updatedCharacter.Intelligence;
                    character.Class = updatedCharacter.Class;

                    // Above 6 piece of code can be written using mapper
                    // but it might create problem for future value such as victory status or defeat status because it 
                    // will change it to default values that we dont want
                    // _mapper.Map<updatedCharacter, Character>();

                    await _context.SaveChangesAsync();

                    serviceReposnse.Data = _mapper.Map<GetCharacterDto>(character);
                }
                else 
                {
                    serviceReposnse.Success = false;
                    serviceReposnse.Message = "Character not found";   
                }

                
            }
            catch (Exception ex)
            {
                serviceReposnse.Success = false;
                serviceReposnse.Message = ex.Message;
            }
            return serviceReposnse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> AddCharacterSkill(AddCharacterSkillDto newCharacterSkill)
        {
            var response = new ServiceResponse<GetCharacterDto>();
            try
            { 
                var character = await _context.Characters
                    .Include(c => c.Weapon)
                    .Include(c => c.Skills)
                    .FirstOrDefaultAsync(c => c.Id == newCharacterSkill.CharacterId && c.User.Id == GetUserId());

                if(character == null)
                {
                    response.Success = false;
                    response.Message = "Character not found.";
                    return response;
                }

                var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == newCharacterSkill.SkillId);
                if(skill == null)
                {
                    response.Success = false;
                    response.Message = "Skill not found.";
                    return response;
                }

                character.Skills.Add(skill);
                await _context.SaveChangesAsync();
                response.Data = _mapper.Map<GetCharacterDto>(character);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }   
            return response;
        }
    }
}