import React, { Component } from 'react';
import { formatSeconds, formatBytes } from './util';
import { Input, Button, Card, Table, Icon, List } from 'semantic-ui-react';

class FileList extends Component {
    render() {
        return (
            <List>
                <List.Item>
                    <List.Icon name='folder open'/>
                    <List.Content>
                        <List.Header>Folder/name/here</List.Header>
                        <List>
                            <List.Item>
                            <Table singleLine>
                            <Table.Header>
                                <Table.Row>
                                    <Table.HeaderCell>File</Table.HeaderCell>
                                    <Table.HeaderCell>Bitrate</Table.HeaderCell>
                                    <Table.HeaderCell>Length</Table.HeaderCell>
                                    <Table.HeaderCell>Size</Table.HeaderCell>
                                </Table.Row>
                            </Table.Header>                                
                            <Table.Body>
                                {this.props.files.map(f => 
                                    <Table.Row>
                                        <Table.Cell>{f.filename.split('\\').pop().split('/').pop()}</Table.Cell>
                                        <Table.Cell>{f.bitrate}</Table.Cell>
                                        <Table.Cell>{formatSeconds(f.length)}</Table.Cell>
                                        <Table.Cell>{formatBytes(f.size)}</Table.Cell>
                                    </Table.Row>
                                )}
                            </Table.Body>
                        </Table>
                            </List.Item>
                        </List>
                    </List.Content>
                </List.Item>
            </List>
        )
    }
}

export default FileList;
