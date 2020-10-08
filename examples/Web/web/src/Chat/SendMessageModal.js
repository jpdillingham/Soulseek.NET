import React, { useEffect } from 'react';
import './Chat.css';

import {
  Icon, Button, Modal, TextArea, Form, Header, Input
} from 'semantic-ui-react';

const SendMessageModal = ({ initiateMessage, ...rest }) => {
  const [open, setOpen] = React.useState(false);
  const [username, setUsername] = React.useState('');
  const [message, setMessage] = React.useState('');
  const [focused, setFocus] = React.useState(false);

  const usernameRef = React.createRef();

  useEffect(() => {
    if (!!usernameRef.current && !focused) {
      usernameRef.current.focus();
      setFocus(true);
    }
  }, [usernameRef, focused]);

  const sendMessage = async () => {
    await initiateMessage(username, message);
    setOpen(false);
  }

  const validInput = () => {
    return username.length > 0 && message.length > 0
  }

  return (
    <Modal
      open={open}
      onClose={() => setOpen(false)}
      onOpen={() => setOpen(true)}
      {...rest}
    >
      <Header>
        <Icon name='send'/>
        <Modal.Content>Send Private Message</Modal.Content>
      </Header>
      <Modal.Content>
        <Form>
          <Form.Field>
              <Input
                ref={usernameRef}
                placeholder='Username' 
                onChange={(e, data) => setUsername(data.value)}
              />
          </Form.Field>
          <Form.Field>
            <TextArea
              placeholder='Message'
              onChange={(e, data) => setMessage(data.value)}
            />
          </Form.Field>
        </Form>
      </Modal.Content>
      <Modal.Actions>
        <Button onClick={() => setOpen(false)}>Cancel</Button>
        <Button 
          positive 
          onClick={() => sendMessage()}
          disabled={!validInput()}
        >Send</Button>
      </Modal.Actions>
    </Modal>
  )
}

export default SendMessageModal;