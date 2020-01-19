import React from 'react';
import axios from 'axios';
import { BASE_URL } from './constants';

import {
    Checkbox
} from 'semantic-ui-react';

import { formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Progress,
    Button
} from 'semantic-ui-react';

const getColor = (state) => {
    switch(state) {
        case 'InProgress':
            return 'blue'; 
        case 'Completed, Succeeded':
            return 'green';
        case 'Requested':
        case 'Queued':
            return '';
        case 'Initializing':
            return 'teal';
        default:
            return 'red';
    }
}

const downloadOne = (username, file) => {
    return axios.post(`${BASE_URL}/transfers/downloads/${username}/${encodeURI(file.filename)}`);
}

const cancel = (direction, username, file) => {
    return axios.delete(`${BASE_URL}/transfers/${direction}s/${username}/${encodeURI(file.filename)}`);
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
                        <Table.HeaderCell className='transferlist-selector'>
                            <Checkbox 
                                fitted 
                                checked={false}
                            />
                        </Table.HeaderCell>
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
                            <Table.Cell className='transferlist-selector'>
                                <Checkbox 
                                    fitted 
                                    checked={f.selected}
                                />
                            </Table.Cell>
                            <Table.Cell className='transferlist-filename'>{getFileName(f.filename)}</Table.Cell>
                            <Table.Cell className='transferlist-size'>{formatBytes(f.bytesTransferred).split(' ', 1) + '/' + formatBytes(f.size)}</Table.Cell>
                            <Table.Cell className='transferlist-progress'>
                                {f.state === 'InProgress' ? <Progress 
                                    style={{ margin: 0 }}
                                    percent={Math.round(f.percentComplete)} 
                                    progress color={getColor(f.state)}
                                /> : <Button fluid size='mini' style={{ margin: 0, padding: 8 }} color={getColor(f.state)}>{f.state}</Button>}
                            </Table.Cell>
                            {direction === 'download' && <Table.Cell className='transferlist-retry'><Button size='mini' style={{ padding: 8 }} onClick={() => downloadOne(username, f)}>Retry</Button></Table.Cell>}
                            <Table.Cell className='transferlist-cancel'><Button size='mini' style={{ padding: 8 }} onClick={() => cancel(direction, username, f)}>Cancel</Button></Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default TransferList;
