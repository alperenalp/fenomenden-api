using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fenomendenAPI.Data;
using fenomendenAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fenomendenAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly DataContext _context;
        public VehicleController(DataContext context)
        {
            _context = context;
        }

        [HttpGet, Authorize(Roles ="standart")]
        public async Task<ActionResult<List<Vehicle>>> Get()
        {
            return Ok(await _context.vehicles.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> Get(int id)
        {
            var vehicle = await _context.vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound("Vehicle Not Found!");
            }
            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<ActionResult<Vehicle>> AddVehicle(Vehicle vehicle)
        {
            _context.vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            var dbVehicle = await _context.vehicles.FindAsync(vehicle.Id);

            return Ok(dbVehicle);
        }

        [HttpPut]
        public async Task<ActionResult<Vehicle>> UpdateVehicle(Vehicle vehicle)
        {
            var dbVehicle = await _context.vehicles.FindAsync(vehicle.Id);
            if (dbVehicle == null)
            {
                return NotFound("Vehicle Not Found!");
            }

            dbVehicle.Name = vehicle.Name;
            dbVehicle.Description=vehicle.Description;
            dbVehicle.CategoryId=vehicle.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(dbVehicle);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<List<Vehicle>>> Delete(int id){
            var dbVehicle = await _context.vehicles.FindAsync(id);
            if (dbVehicle == null)
            {
                return NotFound("Vehicle Not Found!");
            }
            _context.vehicles.Remove(dbVehicle);
            await _context.SaveChangesAsync();
            return Ok(await _context.vehicles.ToListAsync());
        }
    }
}