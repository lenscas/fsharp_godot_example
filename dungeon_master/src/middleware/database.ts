import { Pool, PoolClient, PoolConfig } from 'pg';

import con from '../../database.json';

export class ClientWrapper {
    onTransactionError: 'rollback' | 'commit' = 'rollback';
    onTransactionSuccess: 'rollback' | 'commit' = 'commit';
    client: PoolClient;

    constructor(client: PoolClient | ClientWrapper) {
        if ('client' in client) {
            this.client = client.client;
        } else {
            this.client = client;
        }
    }
    query = (
        query: string,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        bindings: any[],
    ): Promise<{
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        rows: any[];
    }> => this.client.query(query, bindings);

    newLayer = (): ClientWrapper => new ClientWrapper(this);

    startTransaction = async <T>(func: (client: ClientWrapper) => Promise<T>): Promise<T> => {
        this.onTransactionError = 'rollback';
        this.onTransactionSuccess = 'commit';
        await this.client.query('BEGIN');
        try {
            const res = await func(this);
            await this.client.query(this.onTransactionSuccess);
            return res;
        } catch (e) {
            await this.client.query(this.onTransactionError);
            throw e;
        }
    };
}

export class PoolWrapper extends Pool {
    constructor(config: PoolConfig) {
        super(config);
    }
    getCon = async <T>(func: (client: ClientWrapper) => Promise<T>): Promise<T> => {
        const connection = await this.connect();
        const wrapper = new ClientWrapper(connection);
        try {
            const res = await func(wrapper);
            return res;
        } finally {
            connection.release();
        }
    };
    startTransaction = async <T>(func: (client: ClientWrapper) => Promise<T>): Promise<T> => {
        return this.getCon((v) => v.startTransaction(func));
    };
}

let pool: PoolWrapper | null = null;

export const getPool = (): PoolWrapper => {
    if (!pool) {
        pool = new PoolWrapper(con.dev);
    }
    return pool;
};
