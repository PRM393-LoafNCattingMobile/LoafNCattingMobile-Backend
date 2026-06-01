using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class TableStatusRepository(LoafNcattingDbContext context) : GenericRepository<TableStatus>(context), ITableStatusRepository { }

