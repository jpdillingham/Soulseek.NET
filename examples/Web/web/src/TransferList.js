import React from 'react';
import axios from 'axios';
import { BASE_URL } from './constants';

import { formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Progress
} from 'semantic-ui-react';

const getColor = (state) => {
    switch(state) {
        case 'InProgress':
            return 'blue'; 
        case 'Completed, Succeeded':
            return 'green';
        case 'Queued':
            return 'grey';
        case 'Initializing':
            return 'teal';
        default:
            return 'red';
    }
}

const downloadOne = (username, file) => {
    return axios.post(`${BASE_URL}/files/queue/${username}/${encodeURI(file.filename)}`);
}

const cancel = (username, file) => {
    return axios.delete(`${BASE_URL}/files/${username}/${encodeURI(file.filename)}`);
}

const TransferList = ({ username, directoryName, files, direction }) => (
    <div>
        <Header 
            size='small' 
            className='filelist-header'
        >
            <Icon name='folder'/>{directoryName}
        </Header>
        <List>
            <List.Item>
            <Table>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell className='transferlist-filename'>File</Table.HeaderCell>
                        <Table.HeaderCell className='transferlist-size'>Size</Table.HeaderCell>
                        <Table.HeaderCell className='transferlist-progress'>Progress</Table.HeaderCell>
                        {direction === 'download' && <Table.HeaderCell className='transferlist-retry'>Retry</Table.HeaderCell>}
                        <Table.HeaderCell className='transferlist-cancel'>Cancel</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>                                
                <Table.Body>
                    {files.sort((a, b) => getFileName(a.filename).localeCompare(getFileName(b.filename))).map((f, i) => 
                        <Table.Row key={i}>
                            <Table.Cell className='transferlist-filename'>{getFileName(f.filename)}</Table.Cell>
                            <Table.Cell className='transferlist-size'>{formatBytes(f.bytesTransferred).split(' ', 1) + '/' + formatBytes(f.size)}</Table.Cell>
                            <Table.Cell className='transferlist-progress'>
                                <Progress 
                                    style={{ margin: 0}}
                                    percent={Math.round(f.percentComplete)} 
                                    progress color={getColor(f.state)}
                                />
                            </Table.Cell>
                            {direction === 'download' && <Table.Cell className='transferlist-retry'><a onClick={() => downloadOne(username, f)}>Retry</a></Table.Cell>}
                            <Table.Cell className='transferlist-cancel'><a onClick={() => cancel(username, f)}>Cancel</a></Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default TransferList;
