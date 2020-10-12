import React, { useEffect } from 'react';
import api from '../api';
import './Rooms.css';

import {
  Icon, Button, Modal, Table, Header
} from 'semantic-ui-react';

const JoinRoomModal = ({ joinRoom: parentJoinRoom, ...rest }) => {
  const [open, setOpen] = React.useState(false);
  const [available, setAvailable] = React.useState([]);
  const [selected, setSelected] = React.useState(undefined);

  useEffect(() => {
    const getAvailableRooms = async () => {
      const available = (await api.get('/rooms/available')).data;
      setAvailable(available);
    }

    getAvailableRooms();
  }, []);

  const joinRoom = async () => {
    await parentJoinRoom(selected);
    setOpen(false);
  }

  const isSelected = (room) => selected === room.name;

  return (
    <Modal
      open={open}
      onClose={() => setOpen(false)}
      onOpen={() => setOpen(true)}
      {...rest}
    >
      <Header>
        <Icon name='send'/>
        <Modal.Content>Join Room</Modal.Content>
      </Header>
      <Modal.Content scrolling>
        <Table celled selectable>
          <Table.Header>
            <Table.Row>
              <Table.HeaderCell>Name</Table.HeaderCell>
              <Table.HeaderCell>Users</Table.HeaderCell>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {available.map((room, index) => 
              <Table.Row
                key={index}
                style={isSelected(room) ? {fontWeight: 'bold'} : {}}
                onClick={() => setSelected(room.name)}
              >
                <Table.Cell positive={isSelected(room)}>{room.name}</Table.Cell>
                <Table.Cell positive={isSelected(room)}>{room.userCount}</Table.Cell>
              </Table.Row>
            )}
          </Table.Body>
        </Table>
      </Modal.Content>
      <Modal.Actions>
        <Button onClick={() => setOpen(false)}>Cancel</Button>
        <Button 
          positive
          onClick={() => joinRoom()}
          disabled={!selected}
        >Join</Button>
      </Modal.Actions>
    </Modal>
  )
}

export default JoinRoomModal;