import React from 'react';

import { formatSeconds, formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
} from 'semantic-ui-react';

const DownloadList = ({ directoryName, files }) => (
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
                        <Table.HeaderCell>File</Table.HeaderCell>
                        <Table.HeaderCell>Size</Table.HeaderCell>
                        <Table.HeaderCell>Length</Table.HeaderCell>
                        <Table.HeaderCell>State</Table.HeaderCell>
                        <Table.HeaderCell>Progress</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>                                
                <Table.Body>
                    {files.map((f, i) => 
                        <Table.Row key={i}>
                            <Table.Cell>{getFileName(f.filename)}</Table.Cell>
                            <Table.Cell>{formatBytes(f.bytesDownloaded) + '/' + formatBytes(f.size)}</Table.Cell>
                            <Table.Cell>{formatSeconds(f.length)}</Table.Cell>
                            <Table.Cell>{f.state}</Table.Cell>
                            <Table.Cell>{f.percentComplete}</Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default DownloadList;
