using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class GenderRepository(LoafNcattingDbContext context) : GenericRepository<Gender>(context), IGenderRepository { }

