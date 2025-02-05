﻿using System.Data;
using Dapper;
using Models;

namespace Infrastructure;

public class BoxRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly string _databaseSchema;

    public BoxRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
        _databaseSchema = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? "testing"
            : "production";
        //RebuildDatabase("testing");
    }

    public async Task<IEnumerable<Box>> Get()
    {
        //TODO: Please refactor this method to use Dapper's multi-mapping feature instead of the foreach loop to avoid certain doom.
        var boxSql = @$"SELECT
                         box_id AS {nameof(Box.Id)},
                         weight AS {nameof(Box.Weight)},
                         colour AS {nameof(Box.Colour)}, 
                         material AS {nameof(Box.Material)}, 
                         created_at AS {nameof(Box.CreatedAt)},
                         stock AS {nameof(Box.Stock)},
                         price AS {nameof(Box.Price)}
                    FROM {_databaseSchema}.boxes";
        var boxes = await _dbConnection.QueryAsync<Box>(boxSql);

        var enumerable = boxes.ToList();
        foreach (var box in enumerable)
        {
            var dimensionsId = await _dbConnection.QuerySingleAsync<Guid>(
                $"SELECT dimensions_id FROM {_databaseSchema}.boxes WHERE box_id = @Id", new { Id = box.Id });
            var dimensionsSql = @$"SELECT
                         dimensions_id AS {nameof(Dimensions.Id)},
                         length AS {nameof(Dimensions.Length)},
                         width AS {nameof(Dimensions.Width)},
                         height AS {nameof(Dimensions.Height)}
                    FROM {_databaseSchema}.dimensions
                    WHERE dimensions_id = @Id";
            box.Dimensions = await _dbConnection.QuerySingleAsync<Dimensions>(dimensionsSql, new { Id = dimensionsId });
        }

        return enumerable;
    }

    public async Task<Box> Get(Guid id)
    {
        var boxSql = @$"SELECT
                         box_id AS {nameof(Box.Id)},
                         weight AS {nameof(Box.Weight)},
                         colour AS {nameof(Box.Colour)}, 
                         material AS {nameof(Box.Material)}, 
                         created_at AS {nameof(Box.CreatedAt)},
                         stock AS {nameof(Box.Stock)},
                         price AS {nameof(Box.Price)}
                    FROM {_databaseSchema}.boxes
                    WHERE box_id = @Id";
        var box = await _dbConnection.QuerySingleAsync<Box>(boxSql, new { Id = id });

        var dimensionsId =
            await _dbConnection.QuerySingleAsync<Guid>(
                $"SELECT dimensions_id FROM {_databaseSchema}.boxes WHERE box_id = @Id", new { Id = id });
        var dimensionsSql = @$"SELECT
                         dimensions_id AS {nameof(Dimensions.Id)},
                         length AS {nameof(Dimensions.Length)},
                         width AS {nameof(Dimensions.Width)},
                         height AS {nameof(Dimensions.Height)}
                    FROM {_databaseSchema}.dimensions
                    WHERE dimensions_id = @Id";
        box.Dimensions = await _dbConnection.QuerySingleAsync<Dimensions>(dimensionsSql, new { Id = dimensionsId });
        return box;
    }

    public async Task<Box> Create(Box box)
    {
        var insertDimensionsSql = @$"INSERT INTO {_databaseSchema}.dimensions (length, width, height) 
                                        VALUES (@Length, @Width, @Height) 
                                        RETURNING
                                            dimensions_id AS {nameof(Box.Dimensions.Id)},
                                            length AS {nameof(Box.Dimensions.Length)},
                                            width AS {nameof(Box.Dimensions.Width)},
                                            height AS {nameof(Box.Dimensions.Height)}
                                        ";
        var dimensions = await _dbConnection.QuerySingleAsync<Dimensions>(insertDimensionsSql, box.Dimensions);

        var sql =
            @$"INSERT INTO {_databaseSchema}.boxes (weight, colour, material, price, stock, dimensions_id, created_at)
                     VALUES (@Weight, @Colour, @Material, @Price, @Stock, @DimensionsID, @CreatedAt)
                     RETURNING 
                         box_id AS {nameof(Box.Id)},
                         weight AS {nameof(Box.Weight)},
                         colour AS {nameof(Box.Colour)}, 
                         material AS {nameof(Box.Material)}, 
                         created_at AS {nameof(Box.CreatedAt)},
                         stock AS {nameof(Box.Stock)},
                         price AS {nameof(Box.Price)}";

        var createdBox = await _dbConnection.QuerySingleAsync<Box>(sql, new
        {
            box.Weight,
            box.Colour,
            box.Material,
            DimensionsID = dimensions.Id,
            box.CreatedAt,
            box.Stock,
            box.Price
        });

        createdBox.Dimensions = dimensions;
        return createdBox;
    }

    public async Task<Box> Update(Box box)
    {
        //TODO: Resolve updating dimensions better
        var sql = @$"UPDATE {_databaseSchema}.boxes 
                     SET weight = @Weight, colour = @Colour, material = @Material, price = @Price, stock = @Stock
                     WHERE box_id = @Id
                     RETURNING 
                         box_id AS {nameof(Box.Id)},
                         weight AS {nameof(Box.Weight)},
                         colour AS {nameof(Box.Colour)}, 
                         material AS {nameof(Box.Material)},  
                         created_at AS {nameof(Box.CreatedAt)},
                         stock AS {nameof(Box.Stock)},
                         price AS {nameof(Box.Price)}";

        var updatedBox = await _dbConnection.QuerySingleAsync<Box>(sql, new
        {
            box.Id,
            box.Weight,
            box.Colour,
            box.Material,
            box.CreatedAt,
            box.Stock,
            box.Price
        });

        var dimensionsId =
            await _dbConnection.QuerySingleAsync<Guid>(
                $"SELECT dimensions_id FROM {_databaseSchema}.boxes WHERE box_id = @Id", new { box.Id });

        var dimensionsSql = @$"UPDATE {_databaseSchema}.dimensions
                              SET length = @Length, width = @Width, height = @Height
                              WHERE dimensions_id = @Id
                              RETURNING 
                                    dimensions_id AS {nameof(Box.Dimensions.Id)},
                                    length AS {nameof(Box.Dimensions.Length)},
                                    width AS {nameof(Box.Dimensions.Width)},
                                    height AS {nameof(Box.Dimensions.Height)}
                              ";

        updatedBox.Dimensions = await _dbConnection.QuerySingleAsync<Dimensions>(dimensionsSql, new
        {
            Id = dimensionsId,
            box.Dimensions.Length,
            box.Dimensions.Width,
            box.Dimensions.Height
        });
        return updatedBox;
    }

    public async Task Delete(Guid id)
    {
        var sql = $"DELETE FROM {_databaseSchema}.boxes WHERE box_id = @Id";
        await _dbConnection.ExecuteAsync(sql, new { Id = id });
    }
}