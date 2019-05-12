import React from 'react';
import { formatSeconds, formatBytes, getFileName } from './util';

import { 
    Header, 
    Table, 
    Icon, 
    List, 
    Checkbox 
} from 'semantic-ui-react';

const FileList = ({ directoryName, files, onSelectionChange }) => (
    <div>
        <Header 
            size='small' 
            className='filelist-header'
        >
            <Icon name='folder'/>{directoryName}
        </Header>
        <List>
            <List.Item>
            <Table singleLine>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell><Checkbox label=''/></Table.HeaderCell>
                        <Table.HeaderCell>File</Table.HeaderCell>
                        <Table.HeaderCell>Bitrate</Table.HeaderCell>
                        <Table.HeaderCell>Length</Table.HeaderCell>
                        <Table.HeaderCell>Size</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>                                
                <Table.Body>
                    {files.map(f => 
                        <Table.Row>
                            <Table.Cell><Checkbox label='' onChange={(event, data) => onSelectionChange(f.filename, data.checked)}/></Table.Cell>
                            <Table.Cell>{getFileName(f.filename)}</Table.Cell>
                            <Table.Cell>{f.bitRate}</Table.Cell>
                            <Table.Cell>{formatSeconds(f.length)}</Table.Cell>
                            <Table.Cell>{formatBytes(f.size)}</Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table>
            </List.Item>
        </List>
    </div>
);

export default FileList;
