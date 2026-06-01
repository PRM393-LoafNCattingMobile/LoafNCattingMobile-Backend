using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class CatStatusRepository(LoafNcattingDbContext context) : GenericRepository<CatStatus>(context), ICatStatusRepository { }

